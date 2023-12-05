// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Developer.Providers.GitHub.Model;
using Octokit;

namespace Microsoft.Developer.Providers.GitHub;

public sealed class GitHubWorkflow(IGitHubRepository repo, Workflow workflow, GitHubClient client, ILogger log) : IGitHubWorkflow
{
    private static readonly Dictionary<string, WorkflowDispatchInput> emptyInputs = [];

    private string? contents;

    private WorkflowYaml? yaml;

    private Dictionary<string, WorkflowDispatchInput>? inputs;

    public IGitHubRepository Repository => repo;

    public Workflow Resource => workflow;

    public string Name => workflow.Name;

    public string Path => workflow.Path;

    public long Id => workflow.Id;

    public string HtmlUrl => workflow.HtmlUrl;

    public string Description => yaml?.Description() ?? $"Runs the {Name} workflow.";

    public async Task Dispatch(IDictionary<string, object> inputs, CancellationToken token)
    {
        var dispatch = new CreateWorkflowDispatch(repo.Resource.DefaultBranch)
        {
            Inputs = inputs
        };

        log.LogInformation("Creating workflow_dispatch event for {org}/{repo}/{path}", repo.Organization.Name, repo.Name, workflow.Path);

        try
        {
            await client.Actions.Workflows.CreateDispatch(repo.Organization.Name, repo.Name, workflow.Id, dispatch);
        }
        catch (ForbiddenException ex)
        {
            log.LogError(ex, "Unable to create workflow_dispatch event for {org}/{repo}/{path}", repo.Organization.Name, repo.Name, workflow.Path);
        }
    }

    public async Task<string> GetContentsAsync(CancellationToken token)
    {
        if (contents is not null)
        {
            return contents;
        }

        log.LogInformation("Getting workflow contents for {org}/{repo}/{path}", repo.Organization.Name, repo.Name, workflow.Path);

        var result = await client.Repository.Content.GetAllContents(repo.Id, workflow.Path);

        if (result is [{ } fileContents])
        {
            contents = fileContents.Content;

            yaml = WorkflowYaml.Deserializer.Deserialize<WorkflowYaml>(contents);

            return contents;
        }

        throw new InvalidOperationException($"Found {result.Count} files for {workflow.Path}");
    }

    public async Task<bool> IsWorkflowDispatchAsync(CancellationToken token)
    {
        var content = await GetContentsAsync(token);

        // TODO: not confident in my regex yet, so try both and complain here
        var isDispatch = content.Contains("workflow_dispatch", StringComparison.OrdinalIgnoreCase);
        var isDispatchRegex = WorkflowYaml.WorkflowDispatchPattern.Match(content).Length > 0;

        if (isDispatch != isDispatchRegex)
        {
            throw new InvalidOperationException($"isDispatch != isDispatchRegex");
        }

        return isDispatchRegex;
    }

    // TODO: do we need to check if this if we're just going to attempt deserializing?
    private static bool HasInputs(string content)
    {
        // TODO: not confident in my regex yet, so try both and complain here
        var hasInputs = content.Contains("inputs:", StringComparison.OrdinalIgnoreCase);
        var hasInputsRegex = WorkflowYaml.WorkflowDispatchWithInputsPattern.Match(content).Length > 0;

        if (hasInputs != hasInputsRegex)
        {
            throw new InvalidOperationException($"hasInputs != hasInputsRegex");
        }

        return hasInputsRegex;
    }

    public async Task<IReadOnlyDictionary<string, WorkflowDispatchInput>> GetInputsAsync(CancellationToken token)
    {
        if (inputs is null)
        {
            var contents = await GetContentsAsync(token);

            if (!await IsWorkflowDispatchAsync(token))
            {
                return emptyInputs;
            }

            if (HasInputs(contents))
            {
                var yml = WorkflowYaml.Deserializer.Deserialize<WorkflowYamlWithInputs>(contents);
                inputs = yml?.On?.WorkflowDispatch?.Inputs;
            }

            inputs ??= emptyInputs;
        }

        return inputs;
    }
}