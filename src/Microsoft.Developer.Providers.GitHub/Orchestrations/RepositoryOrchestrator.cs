// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Developer.Entities;
using Microsoft.Developer.Providers.GitHub.Model;
using Microsoft.DurableTask;

namespace Microsoft.Developer.Providers.GitHub;

[DurableTask(nameof(RepositoryOrchestrator))]
public class RepositoryOrchestrator : TaskOrchestrator<RepositoryOrchestrator.Input, RepositoryOrchestrator.Output>
{
    public record Input(MsDeveloperUserId User, string Organization, NewRepositoryInputs Payload, string? Template = null);

    public record Output(EntityRef? EntityRef);

    public override async Task<Output> RunAsync([OrchestrationTrigger] TaskOrchestrationContext context, Input input)
    {
        var log = context.CreateReplaySafeLogger(nameof(RepositoryOrchestrator));

        log.LogInformation("Starting repository orchestration {org}", input.Organization);

        var repo = await context.CallCreateRepositoryAsync(new CreateRepository.Input(input.User, input.Organization, input.Payload, input.Template));

        log.LogInformation("Created repository '{name}' ({id})", repo.Name, repo.Id);

        var update = new RepositoryPropertiesUpdate
        {
            RepositoryNames = [repo.Name],
            Properties = [
                new CustomPropertyValue
                {
                    PropertyName = CustomProperty.Use,
                    Value = RepositoryUses.Repo
                }
            ]
        };

        var properties = await context.CallConfigureRepositoryAsync(new ConfigureRepository.Input(input.Organization, update));

        log.LogInformation("Configured repository '{name}' ({id})", repo.Name, repo.Id);

        return new(new EntityRef(EntityKind.Repo) { Namespace = input.Organization, Name = repo.Name });
    }
}