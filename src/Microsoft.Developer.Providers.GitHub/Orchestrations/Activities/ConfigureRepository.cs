// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Developer.Providers.GitHub.Model;
using Microsoft.DurableTask;

namespace Microsoft.Developer.Providers.GitHub;

[DurableTask(nameof(ConfigureRepository))]
public class ConfigureRepository(IGitHubAppService app, ILogger<ConfigureRepository> log)
    : TaskActivity<ConfigureRepository.Input, ConfigureRepository.Output>
{
    public record Input(string Organization, RepositoryPropertiesUpdate Properties);

    public record Output(List<RepositoryProperties> Properties);

    public override async Task<Output> RunAsync(TaskActivityContext context, Input input)
    {
        var token = CancellationToken.None;

        if (await app.GetClient(input.Organization, token) is not { } client)
        {
            throw new Exception($"Unable to get client for {input.Organization}");
        }

        foreach (var repo in input.Properties.RepositoryNames)
        {
            log.LogInformation("Setting custom properties on repository {org}/{repo}", input.Organization, repo);
        }

        var properties = await client.SetRepositoryProperties(input.Organization, input.Properties, token);

        return new(properties);
    }
}
