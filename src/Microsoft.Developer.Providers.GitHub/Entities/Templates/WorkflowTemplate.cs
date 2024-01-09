// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Developer.Entities;
using Microsoft.Developer.Providers.GitHub.Model;
using Microsoft.Developer.Providers.JsonSchema;
using Microsoft.Developer.Requests;
using System.Text;

namespace Microsoft.Developer.Providers.GitHub;

public static class WorkflowTemplate
{
    // TODO:
    // trying to Deserialize<<Dictionary<string, object>> creates Dictionary<string, JsonElement>, and Octokit's
    // built-in serializer chokes.  So we need to convert Dictionary<string, JsonElement> to an actual
    // Dictionary<string, object> where object is bool, string, double, etc.
    internal static Dictionary<string, object> GetWorkflowTemplateInputs(this TemplateRequest request)
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


    public static async Task<Entity> ToTemplateEntity(this IGitHubWorkflow workflow, CancellationToken token)
    {
        var entityRef = new EntityRef(EntityKind.Template) { Name = $"{workflow.Repository.Name}-{workflow.FileName}", Namespace = workflow.Repository.Organization.Name };

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
                    && repositories.Where(r => r.Resource.Permissions.Push).Select(r => r.Name) is { } accessibleRepos
                    && accessibleRepos.Any())
                    {
                        input.Type = WorkflowDispatchInputTypes.Choice;
                        input.Options = [.. accessibleRepos.Order()];
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
}
