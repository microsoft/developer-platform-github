// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using Microsoft.Developer.Data;
using Microsoft.Extensions.Caching.Distributed;
using Octokit;
using Octokit.Internal;

namespace Microsoft.Developer.Providers.GitHub;


public sealed class OAuthUserLoginManager(TimeProvider time, IGitHubAppService gh, IDistributedCache cache, ISecretsManager secrets, IMappedUserRepository<MappedUser, GitHubUser> users) : IUserOAuthLoginManager, ILocalUserManager<MappedUser, GitHubUser>
{
    private readonly SimpleJsonSerializer serializer = new();

    private readonly DistributedCacheEntryOptions defaultOptions = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };

    public async Task<string> AddAsync(MsDeveloperUserId user, string redirectUri, CancellationToken token)
    {
        var key = CreateRandomString();

        await cache.SetAsync(key, PendingState.GetBytes(user, redirectUri), defaultOptions, token);

        return key;
    }

    public async Task<string?> RegisterAsync(string code, string state, CancellationToken token)
    {
        if (await cache.GetAsync(state, token) is { } raw && PendingState.TryParse(raw, out var pendingState))
        {
            await cache.RemoveAsync(state, token);

            var accessToken = await gh.CreateAccessToken(code, token);

            if (accessToken is { })
            {
                if (await GitHubUserService.GetClient(accessToken).User.Current() is { } current)
                {
                    var ghUserId = new GitHubUser
                    {
                        Id = current.Id.ToString(),
                        Name = current.Name,
                        Login = current.Login,
                    };

                    var tokenSecretName = CreateRandomString();

                    // token can only be deserialized if serialized with the octokit's serializer
                    var accessTokenJson = serializer.Serialize(accessToken);
                    await secrets.SetSecretAsync(tokenSecretName, accessTokenJson, token);

                    _ = await users.AddAsync(new()
                    {
                        PlatformUser = pendingState.User,
                        LocalUser = ghUserId,
                        OauthTokenSecretName = tokenSecretName,
                        Expiration = time.GetUtcNow().AddSeconds(accessToken.RefreshTokenExpiresIn),
                    }, token);

                    return pendingState.RedirectUri;
                }
            }
        }

        return null;
    }

    private Task<MappedUser?> GetMappedUserAsync(MsDeveloperUserId user, CancellationToken token)
        => users.GetAsync(user, token);

    private async Task<GitHubUser?> GetGitHubUserAsync(MsDeveloperUserId user)
        => await users.GetAsync(user, default) is { } mapped ? mapped.LocalUser : null;

    public async Task RemoveAsync(MsDeveloperUserId user, CancellationToken token)
    {
        if (await users.GetAsync(user, token) is { } mapped)
        {
            await RemoveAsync(mapped, token);
        }
    }

    public async Task RemoveAsync(GitHubUser githubId, CancellationToken token)
    {
        if (await users.GetAsync(githubId, token) is { } mapped)
        {
            await RemoveAsync(mapped, token);
        }
    }

    private async Task RemoveAsync(MappedUser mapped, CancellationToken token)
    {
        if (mapped.OauthTokenSecretName is { } key)
        {
            await secrets.DeleteSecretAsync(key, token);
        }

        _ = await users.RemoveAsync(mapped, token);
    }

    public async Task<OauthToken?> GetTokenAsync(MsDeveloperUserId user, CancellationToken token)
        => await users.GetAsync(user, token) is { } mapped ? await GetTokenAsync(mapped, token) : null;

    public async Task<OauthToken?> GetTokenAsync(GitHubUser githubId, CancellationToken token)
        => await users.GetAsync(githubId, token) is { } mapped ? await GetTokenAsync(mapped, token) : null;

    private async Task<OauthToken?> GetTokenAsync(MappedUser mapped, CancellationToken token)
        => mapped.OauthTokenSecretName is { } key
        && await GetUserTokenAsync(key, token) is { } oauthToken
        && !string.IsNullOrEmpty(oauthToken?.AccessToken) ? oauthToken : null;

    private async Task<OauthToken?> GetUserTokenAsync(string key, CancellationToken token)
        => await secrets.GetSecretAsync<string>(key, token) is { } tokenJson ? serializer.Deserialize<OauthToken>(tokenJson) : null;

    Task<GitHubUser?> ILocalUserManager<MappedUser, GitHubUser>.GetLocalUserAsync(MsDeveloperUserId user, CancellationToken token)
      => GetGitHubUserAsync(user);

    async Task<MsDeveloperUserId?> ILocalUserManager<MappedUser, GitHubUser>.GetMsDeveloperUserAsync(GitHubUser user, CancellationToken token)
        => await users.GetAsync(user, token) is { } mapped ? mapped.PlatformUser : null;

    async Task<bool> ILocalUserManager.HasLocalUserAsync(MsDeveloperUserId user, CancellationToken token)
        => await users.GetAsync(user, token) is { };

    Task<MappedUser?> ILocalUserManager<MappedUser, GitHubUser>.GetMappedUserAsync(MsDeveloperUserId user, CancellationToken token)
        => GetMappedUserAsync(user, token);

    private static string CreateRandomString()
    {
        // Characters used to generate a state parameter for GitHub auth callback
        const string StateCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

        Span<char> bytes = stackalloc char[20];
        RandomNumberGenerator.GetItems(StateCharacters, bytes);

        return new string(bytes);
    }

    /// <summary>
    /// State stored temporarily while the user goes through the OAuth flow. This can store information that will be needed when we
    /// get the accounts linked, and will be indexed by a random value.
    /// </summary>
    private sealed class PendingState(MsDeveloperUserId user, string redirectUri)
    {
        public MsDeveloperUserId User => user;

        public string RedirectUri => redirectUri;

        public static byte[] GetBytes(MsDeveloperUserId user, string redirectUri)
        {
            using var stream = new MemoryStream();
            using var binary = new BinaryWriter(stream);

            binary.Write(user.UserId);
            binary.Write(user.TenantId);
            binary.Write(redirectUri);

            return stream.ToArray();
        }

        public static bool TryParse(byte[] bytes, [MaybeNullWhen(false)] out PendingState pending)
        {
            using var stream = new MemoryStream(bytes);
            using var binary = new BinaryReader(stream);

            try
            {
                var userId = binary.ReadString();
                var tenantId = binary.ReadString();
                var redirectUri = binary.ReadString();

                pending = new(new(userId, tenantId), redirectUri);
                return true;
            }
            catch
            {
                pending = default;
                return false;
            }
        }
    }
}
