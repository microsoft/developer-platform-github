// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Octokit;

namespace Microsoft.Developer.Providers.GitHub.Model;

public class CustomProperty
{
    private const string Root = "dev_platform";

    public const string Use = Root;

    public const string Name = $"{Root}_name";

    public const string Creator = $"{Root}_creator";


    public string PropertyName { get; set; } = null!;

    public StringEnum<CustomPropertyType> ValueType { get; set; }

    public string? DefaultValue { get; set; }

    public bool Required { get; set; }

    public string? Description { get; set; }

    public List<string>? AllowedValues { get; set; }
}
