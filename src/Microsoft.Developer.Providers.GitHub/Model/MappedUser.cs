// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Developer.Providers.GitHub.Model;

/// <summary>
/// Tracks the relationship between known <see cref="GitHubUser"/> and <see cref="DevPlatformUser"/>.
/// </summary>
public class MappedUser
{
    public required string Id { get; set; }

    public required string Tenant { get; set; }

    public required GitHubUser GitHubUser { get; set; }

    public required MsDeveloperUserId DevPlatformUser { get; set; }

    /// <summary>
    /// Gets or sets a value that tracks the key to the refresh token stored in the secrets manager.
    /// </summary>
    public string? RefreshTokenKey { get; set; }

    /// <summary>
    /// Gets or sets the time until the refresh token will expire.
    /// </summary>
    public DateTimeOffset Expiration { get; set; }
}
