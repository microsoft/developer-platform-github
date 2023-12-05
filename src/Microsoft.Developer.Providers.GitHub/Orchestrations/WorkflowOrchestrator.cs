// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Developer.Requests;
using Microsoft.DurableTask;

namespace Microsoft.Developer.Providers.GitHub;

[DurableTask(nameof(WorkflowOrchestrator))]
public class WorkflowOrchestrator : TaskOrchestrator<WorkflowOrchestrator.Input, WorkflowOrchestrator.Output>
{
    public record Input(MsDeveloperUserId User, string Organization, string Repository, string Workflow, TemplateRequest Payload);

    public class Output();

    public override async Task<Output> RunAsync([OrchestrationTrigger] TaskOrchestrationContext context, Input input)
    {
        const double expireAfterMins = 60;

        var log = context.CreateReplaySafeLogger(nameof(WorkflowOrchestrator));

        log.LogInformation("Starting workflow orchestration for {org}/{repo}/{workflow}", input.Organization, input.Repository, input.Workflow);

        var output = await context.CallStartWorkflowAsync(new StartWorkflow.Input(input.User, input.Organization, input.Repository, input.Workflow, input.Payload));

        log.LogInformation("Started workflow run '{runId}'", output.Run);

        var expire = context.CurrentUtcDateTime.AddMinutes(expireAfterMins);

        while (context.CurrentUtcDateTime < expire)
        {
            var status = await context.CallCheckWorkflowAsync(new CheckWorkflow.Input(input.Organization, input.Repository, output.Run));

            log.LogInformation("Workflow run '{runId}' is '{status}'", output.Run, status.Status);

            if (status.Completed)
            {
                log.LogInformation("Workflow run '{runId}' completed with '{status}'", output.Run, status.Conclusion);

                return new();
            }

            // Wait for the next checkpoint
            var nextCheckpoint = context.CurrentUtcDateTime.Add(status.Wait);
            log.LogInformation("Workflow run '{runId}' not finished waiting {wait} seconds.", output.Run, status.Wait.TotalSeconds);
            log.LogInformation("Next check for workflow run '{runId}' at {checkpoint}.", output.Run, nextCheckpoint);

            await context.CreateTimer(nextCheckpoint, CancellationToken.None);
        }

        log.LogError("Monitor for workflow run '{runId}' expiring after {total} mins.", output.Run, expireAfterMins);

        return new();
    }
}