// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Developer.Providers.GitHub.Model;

namespace Microsoft.Developer.Providers.GitHub;

public interface IGitHubWorkflow : IGitHubResource<Octokit.Workflow>
{
    IGitHubRepository Repository { get; }

    Task<string> GetContentsAsync(CancellationToken token);

    Task<bool> IsWorkflowDispatchAsync(CancellationToken token);

    string Path { get; }

    string FileName => Path.Split('/').Last();

    Task Dispatch(IDictionary<string, object> inputs, CancellationToken token);

    Task<IReadOnlyDictionary<string, WorkflowDispatchInput>> GetInputsAsync(CancellationToken token);
}
