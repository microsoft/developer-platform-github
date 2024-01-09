// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Reflection;
using Octokit;

namespace Microsoft.Developer.Providers.GitHub;

internal static class GitHubConstants
{
    public const string ProductHeaderName = "MSDev";

    public const string NewRepositoryTemplateName = "new-repo";

    public const string Accepts = "application/vnd.github+json";

    public static readonly string ProductHeaderVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";

    public static ProductHeaderValue ProductHeader => new(ProductHeaderName, ProductHeaderVersion);

    public static ProductHeaderValue InstallationProductHeader(long installationId) => new($"{ProductHeaderName}-Installation{installationId}", ProductHeaderVersion);
}