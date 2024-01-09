// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Developer.Entities;

namespace Microsoft.Developer.Providers.GitHub;

public static class ProviderEntity
{
    public static Entity Create()
    {
        var entity = new Entity(EntityKind.Provider)
        {
            Metadata = new Metadata
            {
                Name = "github.com",
                Title = "GitHub",
                Description = "The GitHub provider..."
            },
            Spec = new Spec
            {
            }
        };

        return entity;
    }
}