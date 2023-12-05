// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Developer.Entities;

namespace Microsoft.Developer.Providers.GitHub;

public static class EntityRefExtensions
{
    public static bool IsEmptyOrDefault(this EntityNamespace entityNamespace)
        => entityNamespace.IsEmpty || entityNamespace.Equals(Entity.Defaults.Namespace);

    public static bool IsTemplate(this EntityRef entityRef) => entityRef.Kind.Equals(EntityKind.Template);

    public static bool IsWorkflowTemplate(this EntityRef entityRef)
        => entityRef.IsTemplate() && (entityRef.Name.EndsWith(".yaml") || entityRef.Name.EndsWith(".yml"));
}