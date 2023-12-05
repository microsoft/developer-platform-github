// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Developer.Features;
using Microsoft.Developer.Providers.GitHub;

namespace Microsoft.Developer.Hosting.Middleware;

internal sealed class DeveloperPlatformGitHubMiddleware(IGitHubInstallationService installations, OAuthUserLoginManager userManager, ILogger<GitHubUserService> logger) : IFunctionsWorkerMiddleware
{
    public Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        if (context.Features.Get<IDeveloperPlatformGitHubFeature>() is null)
        {
            var feature = new Feature
            {
                InstallationService = installations,
                UserService = context.Features.Get<IDeveloperPlatformUserFeature>() is { User: { } user } ? new GitHubUserService(installations, userManager, user, logger) : null,
            };

            context.Features.Set<IDeveloperPlatformGitHubFeature>(feature);
        }

        return next(context);
    }

    private sealed class Feature : IDeveloperPlatformGitHubFeature
    {
        public required IGitHubInstallationService InstallationService { get; init; }

        public IGitHubUserService? UserService { get; init; }
    }
}
