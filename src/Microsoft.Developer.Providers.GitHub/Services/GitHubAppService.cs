// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using GitHubJwt;
using Microsoft.Extensions.Options;
using Octokit;
using System.Collections.Concurrent;

namespace Microsoft.Developer.Providers.GitHub;

public class GitHubAppService(ISecretsManager secrets, IOptions<GitHubOptions> options, ILogger<GitHubAppService> log) : IGitHubAppService
{
    private readonly ConcurrentDictionary<string, long> installationMap = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<long, (GitHubClient client, DateTimeOffset expiresAt)> clientMap = new();

    public async Task<GitHubClient> GetClient(CancellationToken token)
    {
        // check for a cached client for the app, and ensure it's valid for the next 5 mins (they expire after 10 mins)
        if (clientMap.TryGetValue(0, out var c) && c is var (client, expiresAt) && expiresAt > DateTimeOffset.Now.AddMinutes(5))
        {
            log.LogInformation("Using cached GitHub app client");
            return client;
        }

        var jwtToken = await CreateEncodedJwtToken(token);

        if (string.IsNullOrEmpty(jwtToken))
        {
            throw new Exception("Failed to get GitHub app jwt token");
        }

        var appClient = new GitHubClient(GitHubConstants.ProductHeader)
        {
            Credentials = new Credentials(jwtToken, AuthenticationType.Bearer)
        };

        clientMap[0] = (appClient, DateTimeOffset.Now.AddMinutes(10));

        log.LogInformation("Created new GitHub app client");
        return appClient;
    }

    public async Task<GitHubClient?> GetClient(string org, CancellationToken token)
        => await GetInstallationId(org, token) is long id ? await GetInstallationClient(id, token) : null;

    public async Task<Uri> GetGitHubLoginUrl(Uri redirectUri, string state, CancellationToken token)
    {
        var client = await GetClient(token);

        return client.Oauth.GetGitHubLoginUrl(new OauthLoginRequest(options.Value.ClientId)
        {
            State = state,
            RedirectUri = redirectUri,
        });
    }

    public async Task<OauthToken?> CreateAccessToken(string code, CancellationToken token)
    {
        var client = await GetClient(token);

        return await client.Oauth
            .CreateAccessToken(new OauthTokenRequest(options.Value.ClientId, options.Value.ClientSecret, code));
    }

    private async Task<GitHubClient?> GetInstallationClient(long installationId, CancellationToken token)
    {
        // check for a cached client for the installation, and ensure it's valid for the next 15 mins (they expire after 1 hour)
        if (clientMap.TryGetValue(installationId, out var c) && c is var (client, expiresAt) && expiresAt > DateTimeOffset.Now.AddMinutes(15))
        {
            log.LogInformation("Using cached GitHub app installation client for: {installationId}", installationId);
            return client;
        }

        // otherwise check if we have a token cached for the installation
        var accessToken = await secrets.GetSecretAsync<AccessToken>($"installation-{installationId}", token);

        // if no token saved or it's not valid for at least 15 more mins
        if (accessToken is null || accessToken.ExpiresAt < DateTimeOffset.Now.AddMinutes(15))
        {
            var appClient = await GetClient(token);

            accessToken = await appClient.GitHubApps.CreateInstallationToken(installationId);

            if (accessToken is null)
            {
                throw new Exception($"Failed to get GitHub app installation token for: {installationId}");
            }

            await secrets.SetSecretAsync($"installation-{installationId}", accessToken, token);
        }

        client = new GitHubClient(GitHubConstants.InstallationProductHeader(installationId))
        {
            Credentials = new Credentials(accessToken.Token)
        };

        clientMap[installationId] = (client, accessToken.ExpiresAt);

        log.LogInformation("Created new GitHub app installation client for: {installationId}", installationId);

        return client;
    }

    private async Task<Installation?> GetInstallation(string org, CancellationToken token)
    {
        log.LogInformation("Getting Installation for {org}", org);

        var app = await GetClient(token);

        try
        {
            if (await app.GitHubApps.GetOrganizationInstallationForCurrent(org) is { } installation)
            {
                installationMap[installation.Account.Login] = installation.Id;
                return installation;
            }

            return null;
        }
        catch (NotFoundException)
        {
            return null;
        }
    }

    private async Task<long?> GetInstallationId(string org, CancellationToken token)
    {
        if (installationMap.TryGetValue(org, out var id) && id is not 0)
        {
            log.LogInformation("Found cached installation id {id} for org {org}", id, org);

            return id;
        }

        return await GetInstallation(org, token) is { } installation ? installation.Id : null;
    }

    private async Task<string> CreateEncodedJwtToken(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(options.Value.AppId))
        {
            throw new ArgumentException("AppId cannot be null or empty.", nameof(options));
        }

        if (string.IsNullOrEmpty(options.Value.PrivateKeyName))
        {
            throw new ArgumentException("PrivateKeyName cannot be null or empty.", nameof(options));
        }

        var key = await secrets.GetSecretAsync<string>(options.Value.PrivateKeyName, cancellationToken)
            ?? throw new InvalidOperationException("Must have GitHub App Pem key before initializing GitHub client");

        var generator = new GitHubJwtFactory(
            new StringPrivateKeySource(key),
            new GitHubJwtFactoryOptions
            {
                AppIntegrationId = Convert.ToInt32(options.Value.AppId), // The GitHub App Id
                ExpirationSeconds = 600 // 10 minutes is the maximum time allowed
            });

        return generator.CreateEncodedJwtToken();
    }
}
