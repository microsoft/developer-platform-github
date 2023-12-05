// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Developer.Entities;
using Microsoft.Developer.Features;

namespace Microsoft.Developer.Providers.GitHub.API;

[RequireLocalUser(Realm = "GitHub")]
[Authorize]
public class GetEntities
{
    private static readonly EntityKind[] supportedKinds = [
        EntityKind.Repo,
        EntityKind.Template
    ];

    [Function(nameof(GetEntities))]
    public async Task<IActionResult> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "entities")] HttpRequest req,
        FunctionContext context, CancellationToken token)
    {
        var log = context.GetLogger<GetEntities>();

        var github = context.Features.GetRequiredFeature<IDeveloperPlatformGitHubFeature>().UserService;

        if (github is null)
        {
            log.LogWarning("Unable to get GitHub service for user.");
            return EntitiesResult.Empty;
        }

        if (await github.GetOrganizations(token) is { } orgs && orgs.Count > 0)
        {
            var entities = await Task.WhenAll(orgs.Select(o => GetAllEntities(o, token)));

            return new EntitiesResult(entities);
        }

        return EntitiesResult.Empty;
    }

    [Function(nameof(GetEntitiesByKind))]
    public async Task<IActionResult> GetEntitiesByKind(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "entities/{kind}")] HttpRequest req,
        string kind, FunctionContext context, CancellationToken token)
    {
        var log = context.GetLogger<GetEntities>();

        var entityRef = context.Features.Get<IDeveloperPlatformRequestFeature>()
            ?? throw new InvalidOperationException("Unable to get EntityRef from context.Features");

        if (!supportedKinds.Contains(entityRef.Kind))
        {
            return EntitiesResult.Empty;
        }

        var github = context.Features.GetRequiredFeature<IDeveloperPlatformGitHubFeature>().UserService;

        if (github is null)
        {
            log.LogWarning("Unable to get GitHub service for user.");
            return EntitiesResult.Empty;
        }

        if (await github.GetOrganizations(token) is { } orgs && orgs.Count > 0)
        {
            if (entityRef.Kind == EntityKind.Repo)
            {
                var repos = await Task.WhenAll(orgs.Select(o => GetRepos(o, token)));
                return new EntitiesResult(repos);
            }

            if (entityRef.Kind == EntityKind.Template)
            {
                var templates = await Task.WhenAll(orgs.Select(o => GetTemplates(o, token)));
                return new EntitiesResult(templates);
            }
        }

        return EntitiesResult.Empty;
    }

    internal static async Task<IEnumerable<Entity>> GetAllEntities(IGitHubOrganization org, CancellationToken token)
    {
        var entities = await Task.WhenAll(GetRepos(org, token), GetTemplates(org, token));

        return entities.SelectMany(e => e);
    }

    internal static async Task<IEnumerable<Entity>> GetRepos(IGitHubOrganization org, CancellationToken token)
    {
        var repos = await org.GetEntityRepositories(token);

        return repos.Select(r => r.ToEntity());
    }

    internal static async Task<IEnumerable<Entity>> GetTemplates(IGitHubOrganization org, CancellationToken token)
    {
        var templates = await Task.WhenAll(GetNewRepositoryTemplates(org, token), GetRepositoryTemplates(org, token), GetWorkflowTemplates(org, token));

        return templates?.SelectMany(t => t) ?? [];
    }

    private static async Task<IEnumerable<Entity>> GetRepositoryTemplates(IGitHubOrganization org, CancellationToken token)
        => (await org.GetTemplateRepositories(token)).Select(t => t.ToTemplateEntity());

    private static async Task<IEnumerable<Entity>> GetNewRepositoryTemplates(IGitHubOrganization org, CancellationToken token)
        => [await org.NewRepositoryTemplateEntity(token)];

    private static async Task<IEnumerable<Entity>> GetWorkflowTemplates(IGitHubOrganization org, CancellationToken token)
    {
        var repos = await org.GetWorkflowRepositories(token);

        var workflows = await Task.WhenAll(repos.Where(r => r.Resource.Permissions.Push).Select(r => r.GetWorkflows(token)));

        return await Task.WhenAll(workflows.SelectMany(l => l).Select(w => w.ToTemplateEntity(token)));
    }
}
