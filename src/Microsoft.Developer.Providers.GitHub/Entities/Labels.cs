// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Developer.Entities;

namespace Microsoft.Developer.Providers.GitHub;

public static class Labels
{
    public const string ProviderId = "github.com";

    public static ProviderKey GetLabelKey(string key) => new(ProviderId, key.ToLowerInvariant());

    public static readonly ProviderKey Org = GetLabelKey("org");
    public static readonly ProviderKey Repo = GetLabelKey("repo");
    public static readonly ProviderKey Id = GetLabelKey("id");
    public static readonly ProviderKey Name = GetLabelKey("name");
    public static readonly ProviderKey Path = GetLabelKey("path");
    public static readonly ProviderKey Workflow = GetLabelKey("workflow");
    public static readonly ProviderKey Url = GetLabelKey("url");
    public static readonly ProviderKey HtmlUrl = GetLabelKey("html-url");

    public static IDictionary<ProviderKey, string> GetDefaults(string org, string repo, IGitHubResource resource)
        => new Dictionary<ProviderKey, string> {
            { Org, org },
            { Repo, repo },
            { Id, resource.Id.ToString() },
            { Name, resource.Name }
    };
}