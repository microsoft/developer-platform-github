// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Developer.Providers.GitHub;

public class GitHubUser : ILocalUser
{
    public required string Id { get; set; }

    public string? Name { get; set; }

    public required string Login { get; set; }
}
