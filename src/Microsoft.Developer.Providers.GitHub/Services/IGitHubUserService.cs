// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Developer.Providers.GitHub.Model;
using Octokit;

namespace Microsoft.Developer.Providers.GitHub;

public interface IGitHubUserService : IGitHubService
{
    IGitHubInstallationService Installations { get; }

    Task<GitHubClient?> GetClient(CancellationToken token);

    Task<GitHubClient?> IGitHubService.GetClient(string org, CancellationToken token) => GetClient(token);

    Task<List<RepositoryProperties>> IGitHubService.GetRepositoryProperties(string org, CancellationToken token)
        => Installations.GetRepositoryProperties(org, token);
}
