// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Developer.Providers.GitHub.Model;
using Octokit;

namespace Microsoft.Developer.Providers.GitHub;

public interface IGitHubService
{
    IGitHubAppService App { get; }

    Task<GitHubClient?> GetClient(string org, CancellationToken token);

    Task<List<string>> GetOrganizationNames(CancellationToken token);

    Task<IGitHubOrganization?> GetOrganization(string org, CancellationToken token);

    Task<List<IGitHubOrganization>> GetOrganizations(CancellationToken token);

    Task<List<RepositoryProperties>> GetRepositoryProperties(string org, CancellationToken token);
}
