// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Developer.Entities;
using Microsoft.Developer.Features;
using Microsoft.Developer.Requests;
using Microsoft.DurableTask.Client;

namespace Microsoft.Developer.Providers.GitHub.API;

[RequireLocalUser(Realm = "GitHub")]
[Authorize]
public class CreateEntities
{
    [Function(nameof(CreateEntitiesFromTemplate))]
    public async Task<IActionResult> CreateEntitiesFromTemplate(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "entities")] HttpRequest req,
        [DurableClient] DurableTaskClient taskClient, FunctionContext context, CancellationToken token)
    {
        var payload = await req.GetTemplateRequestAsync(token);
        var templateRef = payload.TemplateRef;

        // namespace is always the github organization
        if (templateRef.Namespace.IsEmpty || templateRef.Namespace.Equals(Entity.Defaults.Namespace))
        {
            return new BadRequestObjectResult($"Invalid namespace '{templateRef.Namespace}'.");
        }

        if (context.Features.GetRequiredFeature<IDeveloperPlatformGitHubFeature>().UserService is not { } github)
        {
            return new BadRequestObjectResult("Unable to get GitHub service for user.");
        }

        if (await github.GetOrganization(templateRef.Namespace, token) is not { } org)
        {
            return new NotFoundObjectResult($"GitHub organization '{templateRef.Namespace}' not found.");
        }

        var user = context.Features.GetRequiredFeature<IDeveloperPlatformUserFeature>().User;

        string? instanceId = null;

        if (await org.IsWorkflowTemplateEntity(templateRef, token) is (true, { } repoName, { } workflowFileName))
        {
            var input = new WorkflowOrchestrator.Input(user, org.Name, repoName, workflowFileName, payload);

            instanceId = await taskClient.ScheduleNewOrchestrationInstanceAsync(nameof(WorkflowOrchestrator), input, token);
        }
        else if (await org.GetTemplateRepoTemplateEntity(templateRef, token) is { } template)
        {
            var repoTemplate = templateRef.Name.Equals(GitHubConstants.NewRepositoryTemplateName) ? null : templateRef.Name;

            var input = new RepositoryOrchestrator.Input(user, org.Name, payload.GetNewRepoTemplateInputs(), repoTemplate);

            instanceId = await taskClient.ScheduleNewOrchestrationInstanceAsync(nameof(RepositoryOrchestrator), input, token);
        }

        if (instanceId is not null)
        {
            return new AcceptedResult(req.HttpContext.GetStatusUri(instanceId), new TemplateResponse());
        }

        return new BadRequestObjectResult($"Unhandled Template");
    }
}
