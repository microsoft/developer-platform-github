// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Octokit;

namespace Microsoft.Developer.Providers.GitHub;

public sealed class GitHubRepository(IGitHubOrganization org, Repository ghRepo, GitHubClient client, ILogger log) : IGitHubRepository
{
    private IList<IGitHubWorkflow>? workflows;

    private List<IGitHubWorkflow>? allWorkflows;

    private List<string>? environments;

    public IGitHubOrganization Organization => org;

    public Repository Resource => ghRepo;

    public string Name => ghRepo.Name;

    public long Id => ghRepo.Id;

    public string HtmlUrl => ghRepo.HtmlUrl;

    public string Description => ghRepo.Description;

    public async Task<List<string>> GetEnvironments(CancellationToken token)
    {
        if (environments is null)
        {
            log.LogInformation("Getting environments for {org}/{repo}", Organization.Name, Name);

            try
            {
                environments = (await client.Repository.Environment.GetAll(ghRepo.Id))
                    .Environments.Select(e => e.Name).ToList();
            }
            catch (NotFoundException)
            {
                environments = [];
            }
        }

        return environments;
    }

    public async Task<IGitHubWorkflow?> GetWorkflow(string workflowFileName, CancellationToken cancellationToken)
    {
        var workflow = allWorkflows?.SingleOrDefault(r => r.Name.Equals(workflowFileName, StringComparison.OrdinalIgnoreCase))
            ?? workflows?.SingleOrDefault(r => r.Name.Equals(workflowFileName, StringComparison.OrdinalIgnoreCase));

        if (workflow is null)
        {
            log.LogInformation("Getting workflow '{workflowFileName}' for {org}/{repo}", workflowFileName, Organization.Name, Name);

            var ghWorkflow = await client.Actions.Workflows.Get(Organization.Name, Name, workflowFileName);

            workflow = new GitHubWorkflow(this, ghWorkflow, client, log);

            workflows ??= [];
            workflows.Add(workflow);
        }

        return workflow;
    }

    public async Task<List<IGitHubWorkflow>> GetWorkflows(CancellationToken token)
    {
        if (allWorkflows is null)
        {
            log.LogInformation("Getting workflows for {org}/{repo}", Organization.Name, Name);

            var response = await client.Actions.Workflows.List(Organization.Name, Name);

            allWorkflows = [.. response.Workflows.Select(w => new GitHubWorkflow(this, w, client, log))];
        }

        return allWorkflows;
    }
}

