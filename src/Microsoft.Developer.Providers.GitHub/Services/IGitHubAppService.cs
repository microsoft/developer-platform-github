// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Octokit;

namespace Microsoft.Developer.Providers.GitHub;

public interface IGitHubAppService
{
    Task<GitHubClient> GetClient(CancellationToken token);

    Task<GitHubClient?> GetClient(string org, CancellationToken token);

    Task<Uri> GetGitHubLoginUrl(Uri redirectUri, string state, CancellationToken token);

    Task<OauthToken?> CreateAccessToken(string code, CancellationToken token);
}
