// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Developer.Entities;
using Microsoft.Developer.Providers.GitHub.Model;
using Microsoft.Developer.Providers.JsonSchema;
using Microsoft.Developer.Requests;
using System.Text;

namespace Microsoft.Developer.Providers.GitHub;

public static class GithubEntityExtensions
{
    // TODO:
    // trying to Deserialize<<Dictionary<string, object>> creates Dictionary<string, JsonElement>, and Octokit's
    // built-in serializer chokes.  So we need to convert Dictionary<string, JsonElement> to an actual
    // Dictionary<string, object> where object is bool, string, double, etc.
    internal static Dictionary<string, object> GetWorkflowInputs(this TemplateRequest request)
        => request.GetInputDictionary().ToDictionary(j => j.Key, j => GetWorkflowInputValue(j.Value)) is Dictionary<string, object> inputs
        ? inputs : throw new InvalidOperationException($"Could not deserialize inputs from body InputJason");


    private static object GetWorkflowInputValue(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString()!,
        // github api expects numbers as strings for some reason
        JsonValueKind.Number => element.GetDouble().ToString(),
        JsonValueKind.True or JsonValueKind.False => element.GetBoolean(),
        JsonValueKind.Undefined or JsonValueKind.Object or JsonValueKind.Array or JsonValueKind.Null or
        _ => throw new InvalidOperationException($"ValueKind {element.ValueKind} is not supported.")
    };

    internal static NewRepositoryInputs GetNewRepositoryInputs(this TemplateRequest request)
    {
        var inputs = request.GetInputs();

        var name = inputs.GetRequiredInput("name");
        var isPrivate = inputs.GetInput("private", true);
        var description = inputs.GetInput("description");

        var gitignore = inputs.GetInput("gitignore");
        var license = inputs.GetInput("license");

        return new NewRepositoryInputs(name, isPrivate, description, gitignore, license);
    }

    public static async Task<(bool isWorkflow, string? repoName, string? workflowFileName)> IsWorkflowTemplateEntity(this IGitHubOrganization org, EntityRef entityRef, CancellationToken token)
    {
        if (entityRef.Name.EndsWith(".yaml") || entityRef.Name.EndsWith(".yml"))
        {
            var name = entityRef.Name.ToString();

            var parts = name.Split('-');

            if (parts.Length < 2)
            {
                throw new InvalidOperationException($"bad workflow template name");
            }

            string? repoName = null;
            string? workflowFileName = null;

            if (parts.Length == 2)
            {
                repoName = parts[0];
                workflowFileName = parts[1];
            }
            else
            {
                var properties = (await org.GetRepositoryProperties(token)).Where(p => p.Workflows());

                RepositoryProperties? property = null;

                var count = 0;
                repoName = parts[count];

                while (property is null && count++ < parts.Length - 2)
                {
                    repoName += $"-{parts[count]}";
                    property = properties.FirstOrDefault(p => p.RepositoryName.Equals(repoName, StringComparison.OrdinalIgnoreCase));
                }

                if (property is null)
                {
                    throw new InvalidOperationException($"bad workflow template name");
                }

                workflowFileName = name.Replace($"{repoName}-", string.Empty);
            }

            if (string.IsNullOrEmpty(repoName) || string.IsNullOrEmpty(workflowFileName))
            {
                throw new NotImplementedException($"Resolve how to parse repoNames and workflowNames with dashes (ex: {name})");
            }

            return (true, repoName, workflowFileName);
        }

        return (false, null, null);
    }

    public static async Task<Entity?> GetRepoTemplateEntity(this IGitHubOrganization org, EntityRef entityRef, CancellationToken token)
    {
        // is it our static new repo template?
        if (entityRef.Name == GitHubConstants.NewRepoTemplateName)
        {
            return await org.NewRepositoryTemplateEntity(token);
        }

        // must be a template repository
        if (await org.GetRepository(entityRef.Name, token) is { } templateRepo && templateRepo.Resource.IsTemplate)
        {
            return templateRepo.ToTemplateEntity();
        }

        return null;
    }

    public static async Task<Entity?> GetTemplateEntity(this IGitHubOrganization org, EntityRef entityRef, CancellationToken token)
    {
        if (await org.IsWorkflowTemplateEntity(entityRef, token) is (true, { } repoName, { } workflowFileName))
        {
            if (await org.GetRepository(repoName, token) is { } repo && await repo.GetWorkflow(workflowFileName, token) is { } workflow)
            {
                return await workflow.ToTemplateEntity(token);
            }
        }
        else if (await GetRepoTemplateEntity(org, entityRef, token) is { } templateRepo)
        {
            return templateRepo;
        }

        return null;
    }

    public static async Task<Entity> NewRepositoryTemplateEntity(this IGitHubOrganization organization, CancellationToken token)
    {
        var entityRef = TemplateRef.Create(GitHubConstants.NewRepoTemplateName, organization.Name);

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
                    { Labels.Name, GitHubConstants.NewRepoTemplateName },
                },
                Links = [
                    new Link { Title = "web", Url = $"https://github.com/organizations/{organization.Name}/repositories/new" }
                ]
            },
            Spec = new TemplateSpec
            {
                InputJsonSchema = CreateNewRepositoryInputJsonSchema(gitignores, licenses),
                InputUiSchema = CreateNewRepositoryInputUiSchema(),
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

    public static Entity ToTemplateEntity(this IGitHubRepository repository)
    {
        var entityRef = TemplateRef.Create(repository.Name, repository.Organization.Name);

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
                InputJsonSchema = CreateTemplateRepositoryInputJsonSchema(),
                InputUiSchema = CreateNewRepositoryInputUiSchema(),
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

    public static async Task<Entity> ToTemplateEntity(this IGitHubWorkflow workflow, CancellationToken token)
    {
        var entityRef = TemplateRef.Create($"{workflow.Repository.Name}-{workflow.FileName}", workflow.Repository.Organization.Name);

        _ = await workflow.GetContentsAsync(token);

        var entity = new Entity(EntityKind.Template)
        {
            Metadata = new Metadata
            {
                Name = entityRef.Name,
                Namespace = entityRef.Namespace,
                Title = workflow.Name,
                Description = workflow.Description,
                Labels = Labels.GetDefaults(workflow.Repository.Organization.Name, workflow.Repository.Name, workflow),
                Links = [
                    new Link { Title = "web", Url = workflow.HtmlUrl },
                    new Link { Title = "repo", Url = workflow.Repository.HtmlUrl }
                ],
                Tags = workflow.Repository.Resource.Topics.Count > 0 ? [.. workflow.Repository.Resource.Topics] : null
            },
            Spec = new TemplateSpec
            {
                InputJsonSchema = await workflow.CreateWorkflowInputJsonSchema(token)
            }
        };

        entity.Metadata.Labels.Add(Labels.Path, workflow.Path);
        entity.Metadata.Labels.Add(Labels.Workflow, workflow.FileName);

        return entity;
    }

    private static string GetWorkflowDispatchInputType(WorkflowDispatchInput input)
    {
        var inputType = input.Type?.ToLowerInvariant();

        var type = inputType switch
        {
            WorkflowDispatchInputTypes.Environment or WorkflowDispatchInputTypes.Choice => WorkflowDispatchInputTypes.String,
            WorkflowDispatchInputTypes.String or WorkflowDispatchInputTypes.Boolean or WorkflowDispatchInputTypes.Number => inputType,
            null => throw new InvalidOperationException($"WorkflowDispatchInput has no type value"),
            _ => throw new InvalidOperationException($"Unhandled workflow_dispatch input type {input.Type}")
        };

        if (input.Options is not null && inputType != WorkflowDispatchInputTypes.Choice && inputType != WorkflowDispatchInputTypes.Environment)
        {
            throw new InvalidOperationException($"workflow_dispatch input of type {inputType} should not have Options array");
        }

        return type;
    }

    private static void WriteWorkflowDispatchInput(this Utf8JsonWriter writer, string name, WorkflowDispatchInput input)
        => writer.WriteInputParameter(id: name, type: GetWorkflowDispatchInputType(input), title: name.UppercaseFirst().SplitCamelCase(), description: input.Description, @default: input.Default, options: input.Options);

    private static async Task WriteWorkflowDispatch(this Utf8JsonWriter writer, IGitHubWorkflow workflow, CancellationToken token)
    {
        writer.WriteStartObject();
        writer.WriteType(JsonSchemaTypes.Object);

        if (await workflow.GetInputsAsync(token) is { } inputs && inputs.Count > 0)
        {
            if (inputs.Where(p => p.Value.Required).Select(p => p.Key) is { } required)
            {
                writer.WriteRequiredArray(required);
            }

            writer.WriteStartPropertiesObject();

            var environments = workflow.Repository.GetEnvironments(token);

            foreach (var (key, input) in inputs)
            {
                if (input.Type?.ToLowerInvariant() == WorkflowDispatchInputTypes.Environment)
                {
                    input.Options = [.. (await environments).Order()];
                }

                // TODO: for now we're going to assume an input named 'repository' with type 'string'
                //       wants a repository name, so we get the relevant repos and create a pick list
                if (input.Type?.ToLowerInvariant() == WorkflowDispatchInputTypes.String && key.Equals("repository", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (await workflow.Repository.Organization.GetEntityRepositories(token) is { } repositories
                    && repositories.Where(r => r.Resource.Permissions.Push).Select(r => r.Name) is { } accessableRepos
                    && accessableRepos.Any())
                    {
                        input.Type = WorkflowDispatchInputTypes.Choice;
                        input.Options = [.. accessableRepos.Order()];
                    }
                }

                writer.WriteWorkflowDispatchInput(key, input);
            }
        }
        else
        {
            writer.WriteStartPropertiesObject();
        }

        writer.WriteEndObject(); // end properties
        writer.WriteEndObject(); // end root
    }

    private static void WriteNewRepository(this Utf8JsonWriter writer, List<string>? gitignores = null, List<(string value, string name)>? licenses = null)
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
        writer.WriteBoolianAsOneOf("Private", "Public", "You choose who can see and commit to this repository.", "Anyone on the internet can see this repository. You choose who can commit.");
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

    private static void WriteTemplateRepository(this Utf8JsonWriter writer)
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
        writer.WriteBoolianAsOneOf("Private", "Public", "You choose who can see and commit to this repository.", "Anyone on the internet can see this repository. You choose who can commit.");
        writer.WriteEndObject();

        writer.WriteEndObject(); // end properties
        writer.WriteEndObject(); // end root
    }

    private static async Task<string> CreateWorkflowInputJsonSchema(this IGitHubWorkflow workflow, CancellationToken token)
    {
        if (!await workflow.IsWorkflowDispatchAsync(token))
        {
            throw new ArgumentException($"Not a workflow_dispatch workflow");
        }

        JsonWriterOptions writerOptions = new() { Indented = false, };

        using MemoryStream stream = new();
        using Utf8JsonWriter writer = new(stream, writerOptions);

        await writer.WriteWorkflowDispatch(workflow, token);

        writer.Flush();

        var json = Encoding.UTF8.GetString(stream.ToArray());

        return json;
    }

    private static string CreateNewRepositoryInputJsonSchema(List<string>? gitignores = null, List<(string value, string name)>? licenses = null)
    {
        JsonWriterOptions writerOptions = new() { Indented = false, };

        using MemoryStream stream = new();
        using Utf8JsonWriter writer = new(stream, writerOptions);

        writer.WriteNewRepository(gitignores, licenses);

        writer.Flush();

        var json = Encoding.UTF8.GetString(stream.ToArray());

        return json;
    }

    private static string CreateTemplateRepositoryInputJsonSchema()
    {
        JsonWriterOptions writerOptions = new() { Indented = false, };

        using MemoryStream stream = new();
        using Utf8JsonWriter writer = new(stream, writerOptions);

        writer.WriteTemplateRepository();

        writer.Flush();

        var json = Encoding.UTF8.GetString(stream.ToArray());

        return json;
    }

    private static string CreateNewRepositoryInputUiSchema()
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

internal static class Labels
{
    public const string ProviderId = "github.com";

    public static ProviderKey GetLabelKey(string key) => new(ProviderId, key.ToLowerInvariant());

    public static readonly ProviderKey Org = GetLabelKey("org");
    public static readonly ProviderKey Repo = GetLabelKey("repo");
    public static readonly ProviderKey Id = GetLabelKey("id");
    public static readonly ProviderKey Name = GetLabelKey("name");
    public static readonly ProviderKey Path = GetLabelKey("path");
    public static readonly ProviderKey Workflow = GetLabelKey("workflow");
    public static readonly ProviderKey Url = GetLabelKey("url");
    public static readonly ProviderKey HtmlUrl = GetLabelKey("html-url");

    public static IDictionary<ProviderKey, string> GetDefaults(string org, string repo, IGitHubResource resource)
        => new Dictionary<ProviderKey, string> {
            { Org, org },
            { Repo, repo },
            { Id, resource.Id.ToString() },
            { Name, resource.Name }
    };
}