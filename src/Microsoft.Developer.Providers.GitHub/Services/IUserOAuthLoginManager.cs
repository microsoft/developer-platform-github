// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Octokit;

namespace Microsoft.Developer.Providers.GitHub;

public interface IUserOAuthLoginManager
{
    Task<string> AddAsync(MsDeveloperUserId user, string redirectUri, CancellationToken token);

    Task<string?> RegisterAsync(string code, string state, CancellationToken token);

    Task RemoveAsync(MsDeveloperUserId user, CancellationToken token);

    Task RemoveAsync(GitHubUser githubId, CancellationToken token);

    Task<OauthToken?> GetTokenAsync(MsDeveloperUserId user, CancellationToken token);

    Task<OauthToken?> GetTokenAsync(GitHubUser githubId, CancellationToken token);
}
