// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Developer.Providers.GitHub.Model;

public class WorkflowDispatchInput
{
    public string? Description { get; set; }

    public string? DeprecationMessage { get; set; }

    public bool Required { get; set; }

    public string? Type { get; set; }

    public string? Default { get; set; }

    public List<string>? Options { get; set; }
}
