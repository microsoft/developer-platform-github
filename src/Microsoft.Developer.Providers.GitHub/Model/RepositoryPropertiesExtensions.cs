// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Developer.Providers.GitHub.Model;

public static class RepositoryPropertiesExtensions
{
    public static string Use(this RepositoryProperties properties)
        => properties.Properties.FirstOrDefault(v => v.PropertyName.Equals(CustomProperty.Use))?.Value ?? RepositoryUses.Ignore;

    public static bool Workflows(this RepositoryProperties properties) => properties.Use().Equals(RepositoryUses.Workflows);

    public static bool Config(this RepositoryProperties properties) => properties.Use().Equals(RepositoryUses.Config);

    public static bool Ignore(this RepositoryProperties properties) => properties.Use().Equals(RepositoryUses.Ignore);

    public static bool Repo(this RepositoryProperties properties) => properties.Use().Equals(RepositoryUses.Repo);

    public static bool Template(this RepositoryProperties properties) => properties.Use().Equals(RepositoryUses.Template);
}
