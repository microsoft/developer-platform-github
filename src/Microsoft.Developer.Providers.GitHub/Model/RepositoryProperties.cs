// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Developer.Providers.GitHub.Model;

public class RepositoryProperties
{
    public long RepositoryId { get; set; }

    public string RepositoryName { get; set; } = null!;

    public string RepositoryFullName { get; set; } = null!;

    public List<CustomPropertyValue> Properties { get; set; } = [];
}
