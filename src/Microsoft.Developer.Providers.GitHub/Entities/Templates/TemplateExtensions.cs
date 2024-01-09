// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Developer.Entities;

namespace Microsoft.Developer.Providers.GitHub;

public static class TemplateExtensions
{
    public static async Task<Entity?> GetTemplateEntity(this IGitHubOrganization org, EntityRef entityRef, CancellationToken token)
    {
        if (await org.IsWorkflowTemplateEntity(entityRef, token) is (true, { } repoName, { } workflowFileName))
        {
            if (await org.GetRepository(repoName, token) is { } repo && await repo.GetWorkflow(workflowFileName, token) is { } workflow)
            {
                return await workflow.ToTemplateEntity(token);
            }
        }
        else if (await org.GetTemplateRepoTemplateEntity(entityRef, token) is { } templateRepo)
        {
            return templateRepo;
        }

        return null;
    }
}