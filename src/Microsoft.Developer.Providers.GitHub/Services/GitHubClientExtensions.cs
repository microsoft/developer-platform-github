// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Developer.Providers.GitHub.Model;
using Octokit;

namespace Microsoft.Developer.Providers.GitHub;

public static class GitHubClientExtensions
{
    public static async Task<List<CustomProperty>> GetCustomProperties(this GitHubClient client, string org, CancellationToken token)
    {
        var uri = new Uri($"orgs/{org}/properties/schema", UriKind.Relative);

        var response = await client
            .Connection
            .Get<List<CustomProperty>>(uri, null, GitHubConstants.Accepts, token)
            .ConfigureAwait(false);

        return response.Body;
    }

    public static async Task<List<CustomProperty>> SetCustomProperties(this GitHubClient client, string org, CustomPropertyUpdate properties, CancellationToken token)
    {
        var uri = new Uri($"orgs/{org}/properties/schema", UriKind.Relative);

        var response = await client
            .Connection
            .Patch<List<CustomProperty>>(uri, properties, GitHubConstants.Accepts)
            .ConfigureAwait(false);

        return response.Body;
    }

    public static async Task<CustomProperty> GetCustomProperty(this GitHubClient client, string org, string propertyName, CancellationToken token)
    {
        var uri = new Uri($"orgs/{org}/properties/schema/{propertyName}", UriKind.Relative);

        var response = await client
            .Connection
            .Get<CustomProperty>(uri, null, GitHubConstants.Accepts, token)
            .ConfigureAwait(false);

        return response.Body;
    }

    public static async Task<CustomProperty> SetCustomProperty(this GitHubClient client, string org, CustomProperty property, CancellationToken token)
    {
        var uri = new Uri($"orgs/{org}/properties/schema/{property.PropertyName}", UriKind.Relative);

        var response = await client
            .Connection
            .Put<CustomProperty>(uri, property)
            .ConfigureAwait(false);

        return response.Body;
    }

    public static async Task DeleteCustomProperty(this GitHubClient client, string org, CustomProperty property, CancellationToken token)
    {
        var uri = new Uri($"orgs/{org}/properties/schema/{property.PropertyName}", UriKind.Relative);

        _ = await client
            .Connection
            .Delete(uri)
            .ConfigureAwait(false);
    }

    public static async Task<List<RepositoryProperties>> GetRepositoryProperties(this GitHubClient client, string org, CancellationToken token)
    {
        var uri = new Uri($"orgs/{org}/properties/values", UriKind.Relative);

        var response = await client
            .Connection
            .Get<List<RepositoryProperties>>(uri, null, GitHubConstants.Accepts, token)
            .ConfigureAwait(false);

        return response.Body;
    }

    public static async Task<List<RepositoryProperties>> SetRepositoryProperties(this GitHubClient client, string org, RepositoryPropertiesUpdate properties, CancellationToken token)
    {
        var uri = new Uri($"orgs/{org}/properties/values", UriKind.Relative);

        var response = await client
            .Connection
            .Patch<List<RepositoryProperties>>(uri, properties, GitHubConstants.Accepts)
            .ConfigureAwait(false);

        return response.Body;
    }

    public static async Task<List<CustomPropertyValue>> GetRepositoryProperties(this GitHubClient client, string org, string repo, CancellationToken token)
    {
        var uri = new Uri($"repos/{org}/{repo}/properties/values", UriKind.Relative);

        var response = await client
            .Connection
            .Get<List<CustomPropertyValue>>(uri, null, GitHubConstants.Accepts, token)
            .ConfigureAwait(false);

        return response.Body;
    }
}