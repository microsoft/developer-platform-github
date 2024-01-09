// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Developer.Entities;

namespace Microsoft.Developer.Providers.GitHub;

public static class RepoEntity
{
    public static Entity ToEntity(this IGitHubRepository repository)
    {
        var entityRef = new EntityRef(EntityKind.Repo)
        {
            Name = repository.Name,
            Namespace = repository.Organization.Name
        };

        var entity = new Entity(EntityKind.Repo)
        {
            Metadata = new Metadata
            {
                Name = entityRef.Name,
                Namespace = entityRef.Namespace,
                Title = repository.Name,
                Description = repository.Description,
                Labels = Labels.GetDefaults(repository.Organization.Name, repository.Name, repository),
                Links = [
                    new Link { Title = "web", Url = repository.HtmlUrl }
                ]
            },
            Spec = new Spec
            {
            }
        };

        return entity;
    }
}