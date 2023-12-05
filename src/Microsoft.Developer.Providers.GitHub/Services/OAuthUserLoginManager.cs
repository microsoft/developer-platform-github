// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Developer.Providers.GitHub.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Octokit;
using Octokit.Internal;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace Microsoft.Developer.Providers.GitHub;

public record GitHubUser(long Id, string? Name, string Login);

public sealed class OAuthUserLoginManager(TimeProvider time, IGitHubAppService gh, IDistributedCache cache, ISecretsManager secrets, IDbContextFactory<GitHubDbContext> factory) : IUserOAuthLoginManager, ILocalUserManager<GitHubUser>
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
                    var ghUserId = new GitHubUser(current.Id, current.Name, current.Login);
                    var refreshTokenKey = CreateRandomString();

                    // token can only be deserialized if serialized with the octokit's serializer
                    var accessTokenJson = serializer.Serialize(accessToken);
                    await secrets.SetSecretAsync(refreshTokenKey, accessTokenJson, token);

                    using var context = factory.CreateDbContext();

                    context.Users.Add(new()
                    {
                        Id = pendingState.User.UserId,
                        Tenant = pendingState.User.TenantId,
                        DevPlatformUser = pendingState.User,
                        GitHubUser = ghUserId,
                        RefreshTokenKey = refreshTokenKey,
                        Expiration = time.GetUtcNow().AddSeconds(accessToken.RefreshTokenExpiresIn),
                    });

                    await context.SaveChangesAsync(token);

                    return pendingState.RedirectUri;
                }
            }
        }

        return null;
    }

    private async Task<GitHubUser?> GetGitHubUserAsync(MsDeveloperUserId user)
    {
        using var context = factory.CreateDbContext();

        var result = await Filter(user, context.Users).FirstOrDefaultAsync();

        return result?.GitHubUser;
    }

    public Task RemoveAsync(MsDeveloperUserId user, CancellationToken token)
        => RemoveAsync(query => Filter(user, query), token);

    public Task RemoveAsync(GitHubUser githubId, CancellationToken token)
        => RemoveAsync(query => Filter(githubId, query), token);

    private async Task RemoveAsync(Func<IQueryable<MappedUser>, IQueryable<MappedUser>> filter, CancellationToken token)
    {
        using var context = factory.CreateDbContext();

        if (await filter(context.Users).FirstOrDefaultAsync(token) is { } user)
        {
            if (user.RefreshTokenKey is { } key)
            {
                await secrets.DeleteSecretAsync(key, token);
            }

            context.Users.Remove(user);

            await context.SaveChangesAsync(token);
        }
    }

    public Task<OauthToken?> GetTokenAsync(MsDeveloperUserId user, CancellationToken token)
        => GetTokenAsync(query => Filter(user, query), token);

    public Task<OauthToken?> GetTokenAsync(GitHubUser githubId, CancellationToken token)
        => GetTokenAsync(query => Filter(githubId, query), token);

    private async Task<OauthToken?> GetTokenAsync(Func<IQueryable<MappedUser>, IQueryable<MappedUser>> filter, CancellationToken token)
    {
        using var context = factory.CreateDbContext();

        var key = await filter(context.Users)
            .Select(u => u.RefreshTokenKey)
            .FirstOrDefaultAsync(cancellationToken: token);

        if (key is not null && await GetUserTokenAsync(key, token) is { } oauthToken && !string.IsNullOrEmpty(oauthToken?.AccessToken))
        {
            return oauthToken;
        }

        return null;
    }

    private async Task<OauthToken?> GetUserTokenAsync(string key, CancellationToken token)
        => await secrets.GetSecretAsync<string>(key, token) is { } tokenJson ? serializer.Deserialize<OauthToken>(tokenJson) : null;

    Task<GitHubUser?> ILocalUserManager<GitHubUser>.GetLocalUserAsync(MsDeveloperUserId user, CancellationToken token)
      => GetGitHubUserAsync(user);

    async Task<MsDeveloperUserId?> ILocalUserManager<GitHubUser>.GetMsDeveloperUserAsync(GitHubUser user, CancellationToken token)
    {
        using var context = factory.CreateDbContext();

        return await Filter(user, context.Users)
            .Select(u => u.DevPlatformUser)
            .FirstOrDefaultAsync(token);
    }

    async Task<bool> ILocalUserManager.HasLocalUserAsync(MsDeveloperUserId user, CancellationToken token)
    {
        using var context = factory.CreateDbContext();

        var result = await Filter(user, context.Users)
            .FirstOrDefaultAsync(token);

        return result is { };
    }

    /// <summary>
    /// Filter the query to only include the user with the given MsDeveloperUserId.
    /// </summary>
    private static IQueryable<MappedUser> Filter(MsDeveloperUserId user, IQueryable<MappedUser> query)
        => query.Where(u => u.DevPlatformUser.UserId == user.UserId).WithPartitionKey(user.TenantId);

    /// <summary>
    /// Filter the query to only include the user with the given GitHub ID.
    /// </summary>
    private static IQueryable<MappedUser> Filter(GitHubUser githubId, IQueryable<MappedUser> query)
        => query.Where(u => u.GitHubUser.Id == githubId.Id);

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
