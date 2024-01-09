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
public class GetEntity
{
    private static readonly EntityKind[] supportedKinds = [
        EntityKind.Repo,
        EntityKind.Template
    ];

    [Function(nameof(GetEntity))]
    public async Task<IActionResult> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "entities/{kind}/{namespace}/{name}")] HttpRequest req,
        string kind, string @namespace, string name, FunctionContext context, CancellationToken token)
    {
        var log = context.GetLogger<GetEntity>();

        var entityRef = new EntityRef(kind)
        {
            Name = name,
            Namespace = @namespace
        };

        if (!supportedKinds.Contains(kind))
        {
            return new NotFoundResult();
        }

        if (string.IsNullOrEmpty(@namespace))
        {
            return new BadRequestObjectResult("Unable to get GitHub organization from namespace");
        }

        // namespace is always the github organization
        if (entityRef.Namespace.Equals(Entity.Defaults.Namespace))
        {
            return new BadRequestObjectResult($"Invalid namespace '{entityRef.Namespace}'.");
        }

        var github = context.Features.GetRequiredFeature<IDeveloperPlatformGitHubFeature>().UserService;

        if (github is null)
        {
            log.LogWarning("Unable to get GitHub service for user.");
            return new NotFoundResult();
        }

        var org = await github.GetOrganization(entityRef.Namespace, token);

        if (org is null)
        {
            return new NotFoundResult();
        }

        if (entityRef.Kind == EntityKind.Repo)
        {
            var repository = await org.GetRepository(entityRef.Name, token);
            return repository is null ? new NotFoundResult() : new EntityResult(repository.ToEntity());
        }

        if (entityRef.Kind == EntityKind.Template)
        {
            var template = await org.GetTemplateEntity(entityRef, token);
            return template is null ? new NotFoundResult() : new EntityResult(template);
        }

        return new NotFoundResult();
    }
}
