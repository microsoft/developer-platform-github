// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Developer.Providers.GitHub.Model;
using Octokit;

namespace Microsoft.Developer.Providers.GitHub;

public class GitHubInstallationService(IGitHubAppService app, ILogger<GitHubInstallationService> log) : IGitHubInstallationService
{
    private Dictionary<string, IGitHubOrganization>? organizations;

    public IGitHubAppService App => app;

    public Task<GitHubClient?> GetClient(string org, CancellationToken token) => app.GetClient(org, token);


    public async Task<List<string>> GetOrganizationNames(CancellationToken token)
    {
        var client = await app.GetClient(token);

        if (await client.GitHubApps.GetAllInstallationsForCurrent() is { } installs)
        {
            return installs.Where(i => i.TargetType == AccountType.Organization).Select(i => i.Account.Login).ToList();
        }

        log.LogWarning("Unable to get GitHub installations for app");

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

            if (await GetClient(org, token) is not { })
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

    public async Task<List<RepositoryProperties>> GetRepositoryProperties(string org, CancellationToken token)
    {
        if (await GetClient(org, token) is { } client)
        {
            log.LogInformation("Getting Repository Properties for {org}", org);

            return await client.GetRepositoryProperties(org, token);
        }

        return [];
    }
}
