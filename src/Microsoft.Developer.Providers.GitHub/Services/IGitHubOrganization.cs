// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Developer.Providers.GitHub.Model;
using Octokit;

namespace Microsoft.Developer.Providers.GitHub;

public interface IGitHubOrganization
{
    string Name { get; }

    Task<List<RepositoryProperties>> GetRepositoryProperties(CancellationToken token);

    Task<IGitHubRepository> GetRepository(string repo, CancellationToken token, bool cacheEnvironments = false);

    Task<List<IGitHubRepository>> GetWorkflowRepositories(CancellationToken token);

    Task<List<IGitHubRepository>> GetTemplateRepositories(CancellationToken token);

    Task<List<IGitHubRepository>> GetEntityRepositories(CancellationToken token);

    Task<List<string>> GetGitignoreTemplates(CancellationToken token);

    Task<List<LicenseMetadata>> GetLicenseTemplates(CancellationToken token);
}
