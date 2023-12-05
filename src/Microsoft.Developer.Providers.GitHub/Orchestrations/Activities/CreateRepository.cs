// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Developer.Providers.GitHub.Model;
using Microsoft.DurableTask;
using Octokit;

namespace Microsoft.Developer.Providers.GitHub;

[DurableTask(nameof(CreateRepository))]
public class CreateRepository(IGitHubInstallationService installations, OAuthUserLoginManager userManager, ILogger<CreateRepository> log)
    : TaskActivity<CreateRepository.Input, CreateRepository.Output>
{
    public record Input(MsDeveloperUserId User, string Organization, NewRepositoryInputs Payload, string? Template = null);

    public record Output(string Name, long Id);

    public override async Task<Output> RunAsync(TaskActivityContext context, Input input)
    {
        var token = CancellationToken.None;

        var github = new GitHubUserService(installations, userManager, input.User, log);

        if (await github.GetClient(token) is not { } client)
        {
            log.LogError("Unable to get client for {org}", input.Organization);
            throw new Exception($"Unable to get client for {input.Organization}");
        }

        if (!string.IsNullOrEmpty(input.Template))
        {
            log.LogInformation("Creating new repository from template '{template}': {org}/{repo}", input.Template, input.Organization, input.Payload.Name);

            var repo = await client.Repository
                .Generate(input.Organization, input.Template, new NewRepositoryFromTemplate(input.Payload.Name)
                {
                    Owner = input.Organization,
                    Description = input.Payload.Description,
                    Private = input.Payload.Private
                });

            return new(repo.Name, repo.Id);
        }
        else
        {
            log.LogInformation("Creating new repository: {org}/{repo}", input.Organization, input.Payload.Name);

            var repo = await client.Repository
                .Create(input.Organization, new NewRepository(input.Payload.Name)
                {
                    AutoInit = true,
                    Private = input.Payload.Private,
                    Description = input.Payload.Description ?? "Created by the Developer Platform",
                    GitignoreTemplate = input.Payload.Gitignore,
                    LicenseTemplate = input.Payload.License
                });

            return new(repo.Name, repo.Id);
        }
    }
}
