// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Developer.Providers.GitHub.Model;

public class CustomPropertyValue
{
    public string PropertyName { get; set; } = null!;
    public string? Value { get; set; }
}