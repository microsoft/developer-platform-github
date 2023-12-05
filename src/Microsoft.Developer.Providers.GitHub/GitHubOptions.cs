// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Developer.Providers.GitHub;

public class GitHubOptions
{
    public const string Section = "GitHub";

    public string AppId { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public string ClientSecretName { get; set; } = "github-client-secret";

    public string PrivateKey { get; set; } = string.Empty;

    public string PrivateKeyName { get; set; } = "github-private-key";

    public string WebhookSecret { get; set; } = string.Empty;

    public string WebhookSecretName { get; set; } = "github-webhook-secret";
}
