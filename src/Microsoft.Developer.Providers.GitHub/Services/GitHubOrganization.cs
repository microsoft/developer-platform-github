// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Developer.Providers.GitHub.Model;
using Octokit;

namespace Microsoft.Developer.Providers.GitHub;

public sealed class GitHubOrganization(IGitHubService service, string org, ILogger log) : IGitHubOrganization
{
    private IReadOnlyList<string>? gitignores;
    private IReadOnlyList<LicenseMetadata>? licenses;
    private readonly Dictionary<string, IGitHubRepository> repositories = new(StringComparer.OrdinalIgnoreCase);
    private List<RepositoryProperties>? properties;

    public string Name => org;

    private async Task<GitHubClient> GetClient(CancellationToken token)
    {
        if (await service.GetClient(org, token) is { } client)
        {
            return client;
        }

        log.LogError("Unable to get client for {org}", org);
        throw new InvalidOperationException($"Unable to get client for {org}");
    }

    public async Task<List<RepositoryProperties>> GetRepositoryProperties(CancellationToken token)
    {
        if (properties is null)
        {
            log.LogInformation("Getting custom properties for {org}", Name);

            properties = await service.GetRepositoryProperties(org, token);
        }

        return properties;
    }

    public async Task<IGitHubRepository> GetRepository(string name, CancellationToken token, bool cacheEnvironments = false)
    {
        if (!repositories.TryGetValue(name, out var repo))
        {
            log.LogInformation("Getting repository {org}/{repo}", Name, name);

            var client = await GetClient(token);
            var ghRepo = await client.Repository.Get(org, name);

            repositories[ghRepo.Name] = repo = new GitHubRepository(this, ghRepo, client, log);
        }

        if (cacheEnvironments)
        {
            _ = await repo.GetEnvironments(token);
        }

        return repo;
    }

    public async Task<List<IGitHubRepository>> GetEntityRepositories(CancellationToken token)
    {
        var props = (await GetRepositoryProperties(token)).Where(p => p.Repo());
        var repos = await Task.WhenAll(props.Select(p => GetRepository(p.RepositoryName, token)));

        return [.. repos];
    }

    public async Task<List<IGitHubRepository>> GetWorkflowRepositories(CancellationToken token)
    {
        var props = (await GetRepositoryProperties(token)).Where(p => p.Workflows());
        var repos = await Task.WhenAll(props.Select(p => GetRepository(p.RepositoryName, token, true)));

        return [.. repos];
    }

    public async Task<List<IGitHubRepository>> GetTemplateRepositories(CancellationToken token)
    {
        var props = (await GetRepositoryProperties(token)).Where(p => p.Template());
        var repos = await Task.WhenAll(props.Select(p => GetRepository(p.RepositoryName, token)));

        return [.. repos.Where(r => r.Resource.IsTemplate)];
    }

    public async Task<List<string>> GetGitignoreTemplates(CancellationToken token)
    {
        var client = await GetClient(token);
        gitignores ??= await client.GitIgnore.GetAllGitIgnoreTemplates();
        return [.. gitignores];
    }

    public async Task<List<LicenseMetadata>> GetLicenseTemplates(CancellationToken token)
    {
        var client = await GetClient(token);
        licenses ??= await client.Licenses.GetAllLicenses();
        return [.. licenses];
    }
}