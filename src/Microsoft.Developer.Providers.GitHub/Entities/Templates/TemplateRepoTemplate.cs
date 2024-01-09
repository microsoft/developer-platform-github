// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Developer.Entities;
using Microsoft.Developer.Providers.JsonSchema;
using System.Text;

namespace Microsoft.Developer.Providers.GitHub;

public static class TemplateRepoTemplate
{
    public static async Task<Entity?> GetTemplateRepoTemplateEntity(this IGitHubOrganization org, EntityRef entityRef, CancellationToken token)
    {
        // is it our static new repo template?
        if (entityRef.Name == GitHubConstants.NewRepositoryTemplateName)
        {
            return await org.NewRepoTemplateEntity(token);
        }

        // must be a template repository
        if (await org.GetRepository(entityRef.Name, token) is { } templateRepo && templateRepo.Resource.IsTemplate)
        {
            return templateRepo.ToTemplateEntity();
        }

        return null;
    }

    public static Entity ToTemplateEntity(this IGitHubRepository repository)
    {
        var entityRef = new EntityRef(EntityKind.Template) { Name = repository.Name, Namespace = repository.Organization.Name };

        var entity = new Entity(EntityKind.Template)
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
                ],
                Tags = repository.Resource.Topics.Count > 0 ? [.. repository.Resource.Topics] : null
            },
            Spec = new TemplateSpec
            {
                InputJsonSchema = CreateTemplateRepoTemplateInputJsonSchema(),
                InputUiSchema = CreateTemplateRepoTemplateInputUiSchema(),
                Creates = [
                    new EntityPlan
                    {
                        Kind = EntityKind.Repo,
                        Namespace = entityRef.Namespace,
                    }
                ]
            }
        };

        return entity;
    }

    private static void WriteTemplateRepoTemplate(this Utf8JsonWriter writer)
    {
        writer.WriteStartObject();

        writer.WriteType(JsonSchemaTypes.Object);

        writer.WriteRequiredArray("name");

        writer.WriteStartPropertiesObject();

        writer.WriteInputParameter(id: "name", type: JsonSchemaTypes.String, title: "Name", description: "The name of the new repository.");
        writer.WriteInputParameter(id: "description", type: JsonSchemaTypes.String, title: "Description", description: "A short description of the new repository.");
        // writer.WriteInputParameter(id: "private", type: JsonSchemaTypes.Boolean, title: "Private", description: "Either true to create a new private repository or false to create a new public one.", @default: "true");

        writer.WriteStartObject("private");
        writer.WriteName("private");
        writer.WriteTitle("Visibility");
        writer.WriteType(JsonSchemaTypes.Boolean);
        writer.WriteDefaultAndValue(true);
        writer.WriteBooleanAsOneOf("Private", "Public", "You choose who can see and commit to this repository.", "Anyone on the internet can see this repository. You choose who can commit.");
        writer.WriteEndObject();

        writer.WriteEndObject(); // end properties
        writer.WriteEndObject(); // end root
    }

    private static string CreateTemplateRepoTemplateInputJsonSchema()
    {
        JsonWriterOptions writerOptions = new() { Indented = false, };

        using MemoryStream stream = new();
        using Utf8JsonWriter writer = new(stream, writerOptions);

        writer.WriteTemplateRepoTemplate();

        writer.Flush();

        var json = Encoding.UTF8.GetString(stream.ToArray());

        return json;
    }

    private static string CreateTemplateRepoTemplateInputUiSchema()
    {
        JsonWriterOptions writerOptions = new() { Indented = false, };

        using MemoryStream stream = new();
        using Utf8JsonWriter writer = new(stream, writerOptions);

        writer.WriteStartObject();

        writer.WriteStartObject("name");
        writer.WriteBoolean("ui:autofocus", true);
        writer.WriteEndObject();

        writer.WriteStartObject("private");
        writer.WriteString("ui:widget", "radio");
        writer.WriteEndObject();

        writer.WriteEndObject();

        writer.Flush();

        var json = Encoding.UTF8.GetString(stream.ToArray());

        return json;
    }
}
