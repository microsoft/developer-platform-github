// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Developer.Providers.GitHub.Model;

public class RepositoryPropertiesUpdate
{
    public List<string> RepositoryNames { get; set; } = [];

    public List<CustomPropertyValue> Properties { get; set; } = [];
}