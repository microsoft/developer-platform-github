// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.DurableTask;

namespace Microsoft.Developer.Providers.GitHub;

[DurableTask(nameof(CheckWorkflow))]
public class CheckWorkflow(IGitHubAppService app, ILogger<CheckWorkflow> log)
    : TaskActivity<CheckWorkflow.Input, CheckWorkflow.Output>
{
    public record Input(string Organization, string Repository, long Run);

    public record Output(bool Completed, string Status, string? Conclusion, TimeSpan Wait);

    public override async Task<Output> RunAsync(TaskActivityContext context, Input input)
    {
        var token = CancellationToken.None;

        if (await app.GetClient(input.Organization, token) is not { } client)
        {
            throw new Exception($"Unable to get client for {input.Organization}");
        }

        var run = await client.Actions.Workflows.Runs.Get(input.Organization, input.Repository, input.Run)
            ?? throw new Exception($"Unable to get workflow run {input.Run}");

        var wait = GetWait(run.Status.Value);

        if (run.Status == Octokit.WorkflowRunStatus.Completed)
        {
            log.LogInformation("Workflow run {runId} is completed", input.Run);
            return new Output(true, run.Status.ToString(), run.Conclusion.GetValueOrDefault().ToString(), wait);
        }
        else
        {
            log.LogInformation("Workflow run {runId} is {status}", input.Run, run.Status);
            return new Output(false, run.Status.ToString(), null, wait);
        }
    }

    private TimeSpan GetWait(Octokit.WorkflowRunStatus status) => status switch
    {
        Octokit.WorkflowRunStatus.Requested => TimeSpan.FromSeconds(5), // this should never happen
        Octokit.WorkflowRunStatus.Queued => TimeSpan.FromSeconds(10),
        Octokit.WorkflowRunStatus.Pending => TimeSpan.FromSeconds(10),
        Octokit.WorkflowRunStatus.Waiting => TimeSpan.FromMinutes(10), // waiting for approval
        Octokit.WorkflowRunStatus.InProgress => TimeSpan.FromSeconds(5),
        Octokit.WorkflowRunStatus.Completed => TimeSpan.Zero,
        _ => TimeSpan.FromMinutes(5)
    };
}