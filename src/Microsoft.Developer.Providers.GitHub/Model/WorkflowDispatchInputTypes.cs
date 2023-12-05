// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Developer.Providers.JsonSchema;

namespace Microsoft.Developer.Providers.GitHub.Model;

public static class WorkflowDispatchInputTypes
{
    public const string Boolean = JsonSchemaTypes.Boolean;

    public const string Number = JsonSchemaTypes.Number;

    public const string String = JsonSchemaTypes.String;

    public const string Choice = "choice";

    public const string Environment = "environment";
}
