// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Developer.Providers.GitHub;

public interface IGitHubResource
{
    string Name { get; }

    long Id { get; }

    string HtmlUrl { get; }

    string Description { get; }
}

public interface IGitHubResource<T> : IGitHubResource
{
    T Resource { get; }
}