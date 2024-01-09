// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Developer.Entities;
using Microsoft.Developer.Providers.GitHub.Model;
using Microsoft.Developer.Providers.JsonSchema;
using Microsoft.Developer.Requests;
using System.Text;

namespace Microsoft.Developer.Providers.GitHub;

public static class NewRepoTemplate
{
    public static NewRepositoryInputs GetNewRepoTemplateInputs(this TemplateRequest request)
    {
        var inputs = request.GetInputs();

        var name = inputs.GetRequiredInput("name");
        var isPrivate = inputs.GetInput("private", true);
        var description = inputs.GetInput("description");

        var gitignore = inputs.GetInput("gitignore");
        var license = inputs.GetInput("license");

        return new NewRepositoryInputs(name, isPrivate, description, gitignore, license);
    }

    public static async Task<Entity> NewRepoTemplateEntity(this IGitHubOrganization organization, CancellationToken token)
    {
        var entityRef = new EntityRef(EntityKind.Template) { Name = GitHubConstants.NewRepositoryTemplateName, Namespace = organization.Name };

        var gitignores = await organization.GetGitignoreTemplates(token);
        var licenses = (await organization.GetLicenseTemplates(token))
            .Select(l => (value: l.Key, name: l.Name)).ToList();

        var entity = new Entity(EntityKind.Template)
        {
            Metadata = new Metadata
            {
                Name = entityRef.Name,
                Namespace = entityRef.Namespace,
                Title = "Repository",
                Description = "Creates an empty GitHub repository",
                Labels = {
                    { Labels.Org, organization.Name },
                    { Labels.Name, GitHubConstants.NewRepositoryTemplateName },
                },
                Links = [
                    new Link { Title = "web", Url = $"https://github.com/organizations/{organization.Name}/repositories/new" }
                ]
            },
            Spec = new TemplateSpec
            {
                InputJsonSchema = CreateNewRepoTemplateInputJsonSchema(gitignores, licenses),
                InputUiSchema = CreateNewRepoTemplateInputUiSchema(),
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

    private static void WriteNewRepoTemplate(this Utf8JsonWriter writer, List<string>? gitignores = null, List<(string value, string name)>? licenses = null)
    {
        writer.WriteStartObject();

        writer.WriteType(JsonSchemaTypes.Object);

        writer.WriteRequiredArray("name", "description");

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

        if (gitignores is not null && gitignores.Count > 0)
        {
            writer.WriteInputParameter(id: "gitignore", type: JsonSchemaTypes.String, title: "Add .gitignore", description: "Choose which files not to track from a list of templates.", options: gitignores);
        }

        if (licenses is not null && licenses.Count > 0)
        {
            writer.WriteEnumInputParameter(id: "license", type: JsonSchemaTypes.String, title: "Choose a license", description: "A license tells others what they can and can't do with your code.", options: licenses);
        }

        writer.WriteEndObject(); // end properties
        writer.WriteEndObject(); // end root
    }

    private static string CreateNewRepoTemplateInputJsonSchema(List<string>? gitignores = null, List<(string value, string name)>? licenses = null)
    {
        JsonWriterOptions writerOptions = new() { Indented = false, };

        using MemoryStream stream = new();
        using Utf8JsonWriter writer = new(stream, writerOptions);

        writer.WriteNewRepoTemplate(gitignores, licenses);

        writer.Flush();

        var json = Encoding.UTF8.GetString(stream.ToArray());

        return json;
    }

    private static string CreateNewRepoTemplateInputUiSchema()
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