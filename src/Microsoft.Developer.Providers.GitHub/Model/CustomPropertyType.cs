// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Octokit.Internal;

namespace Microsoft.Developer.Providers.GitHub.Model;

public enum CustomPropertyType
{
    [Parameter(Value = "string")]
    String,

    [Parameter(Value = "single_select")]
    SingleSelect,
}
