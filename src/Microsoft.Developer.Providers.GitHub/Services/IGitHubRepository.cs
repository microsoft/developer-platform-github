// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Developer.Providers.GitHub;

public interface IGitHubRepository : IGitHubResource<Octokit.Repository>
{
    IGitHubOrganization Organization { get; }

    Task<IGitHubWorkflow?> GetWorkflow(string workflowFileName, CancellationToken cancellationToken);

    Task<List<string>> GetEnvironments(CancellationToken token);

    Task<List<IGitHubWorkflow>> GetWorkflows(CancellationToken token);
}
