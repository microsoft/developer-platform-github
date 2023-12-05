// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Developer.Requests;
using Microsoft.DurableTask;

namespace Microsoft.Developer.Providers.GitHub;

[DurableTask(nameof(StartWorkflow))]
public class StartWorkflow(IGitHubInstallationService installations, OAuthUserLoginManager userManager, ILogger<StartWorkflow> log)
    : TaskActivity<StartWorkflow.Input, StartWorkflow.Output>
{
    public record Input(MsDeveloperUserId User, string Organization, string Repository, string Workflow, TemplateRequest Payload);

    public record Output(long Workflow, long Run);

    public override async Task<Output> RunAsync(TaskActivityContext context, Input input)
    {
        var token = CancellationToken.None;

        var github = new GitHubUserService(installations, userManager, input.User, log);

        if (await github.GetOrganization(input.Organization, token) is not { } org
         || await org.GetRepository(input.Repository, token) is not { } repo
         || await repo.GetWorkflow(input.Workflow, token) is not { } workflow)
        {
            throw new Exception($"Unable to get {input.Organization}/{input.Repository}/{input.Workflow}");
        }

        if (await github.GetClient(token) is not { } client)
        {
            throw new Exception($"Unable to get client for {input.Organization}");
        }

        var me = await client.User.Current();

        var request = new Octokit.WorkflowRunsRequest
        {
            Actor = me.Login,
            Event = "workflow_dispatch"
        };

        var preRunsResponse = await client.Actions.Workflows.Runs
            .ListByWorkflow(input.Organization, input.Repository, workflow.Id, request);

        var preRuns = preRunsResponse.WorkflowRuns.OrderByDescending(r => r.CreatedAt).ToList();

        await workflow.Dispatch(input.Payload.GetWorkflowInputs(), token);

        var newRun = default(Octokit.WorkflowRun);

        var count = 0;

        do
        {
            log.LogInformation("Waiting for workflow run to start {count}", count++);

            await Task.Delay(1000);

            var postRunsResponse = await client.Actions.Workflows.Runs
                .ListByWorkflow(input.Organization, input.Repository, workflow.Id, request);

            var postRuns = postRunsResponse.WorkflowRuns.OrderByDescending(r => r.CreatedAt);

            newRun = postRuns.FirstOrDefault(r => !preRuns.Any(pr => pr.Id == r.Id));

        } while (newRun is default(Octokit.WorkflowRun) && count < 10);

        if (newRun is null)
        {
            throw new Exception("Unable to find new workflow run");
        }

        log.LogInformation("Workflow run started");

        return new(workflow.Id, newRun.Id);
    }
}
