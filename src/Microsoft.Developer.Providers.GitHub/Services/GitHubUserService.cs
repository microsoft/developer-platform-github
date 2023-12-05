// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Octokit;

namespace Microsoft.Developer.Providers.GitHub;

public class GitHubUserService(IGitHubInstallationService installations, OAuthUserLoginManager login, MsDeveloperUserId user, ILogger log) : IGitHubUserService
{
    private Dictionary<string, IGitHubOrganization>? organizations;

    public IGitHubAppService App => Installations.App;

    public IGitHubInstallationService Installations => installations;

    private GitHubClient? cachedClient;

    public static GitHubClient GetClient(OauthToken accessToken) => new(GitHubConstants.ProductHeader)
    {
        Credentials = new Credentials(accessToken.AccessToken, AuthenticationType.Bearer)
    };

    public async Task<GitHubClient?> GetClient(CancellationToken token)
    {
        if (cachedClient is not null)
        {
            return cachedClient;
        }

        if (await login.GetTokenAsync(user, token) is { } oauthToken)
        {
            return cachedClient = GetClient(oauthToken);
        }

        log.LogWarning("Unable to get GitHub oauth token for user");
        log.LogWarning("Removing mapped user for {user}", user);
        await login.RemoveAsync(user, token);
        cachedClient = null;

        return null;
    }

    public async Task<List<string>> GetOrganizationNames(CancellationToken token)
    {
        if (await GetClient(token) is { } client)
        {
            try
            {
                if (await client.GitHubApps.GetAllInstallationsForCurrentUser() is { } result)
                {
                    var installs = result.Installations
                        .Where(i => i.TargetType == AccountType.Organization)
                        .Select(i => i.Account.Login).ToList();

                    if (installs.Count == 0)
                    {
                        log.LogWarning("GitHub app is not installed on any organizations for user {user}", user);
                    }

                    return installs;
                }
            }
            catch (AuthorizationException exc)
            {
                log.LogWarning(exc, "GitHub authorization error");
                log.LogWarning("Removing mapped user for {user}", user);
                await login.RemoveAsync(user, token);
                cachedClient = null;
            }
        }

        log.LogWarning("Unable to get GitHub installations for user");

        return [];
    }

    public async Task<List<IGitHubOrganization>> GetOrganizations(CancellationToken token)
    {
        if (organizations is not null)
        {
            return [.. organizations.Values];
        }

        organizations = new(StringComparer.OrdinalIgnoreCase);

        if (await GetOrganizationNames(token) is { } orgs && orgs.Count > 0)
        {
            return [.. await Task.WhenAll(orgs.Select(o => GetOrganization(o, token)))];
        }

        return [];
    }

    public async Task<IGitHubOrganization?> GetOrganization(string org, CancellationToken token)
    {
        organizations ??= new(StringComparer.OrdinalIgnoreCase);

        if (!organizations.TryGetValue(org, out var organization))
        {
            log.LogInformation("Getting organization {org}", org);

            if (await GetClient(token) is not { })
            {
                log.LogError("Unable to get client for {org}", org);
                return null;
            }
            else
            {
                organization = new GitHubOrganization(this, org, log);

                // cache repository properties
                _ = await organization.GetRepositoryProperties(token);

                organizations[org] = organization;
            }
        }

        return organization;
    }
}
