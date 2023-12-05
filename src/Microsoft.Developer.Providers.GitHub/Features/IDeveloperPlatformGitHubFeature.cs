// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Developer.Providers.GitHub;

namespace Microsoft.Developer.Features;

internal interface IDeveloperPlatformGitHubFeature
{
    /// <summary>
    /// An instance of <see cref="IGitHubInstallationService"/> that will authenticate on behalf of the app.
    /// </summary>
    IGitHubInstallationService InstallationService { get; }

    /// <summary>
    /// An instance of <see cref="IGitHubUserService"/> that will authenticate on behalf of the user. If there is no logged in user, this will be <c>null</c>.
    /// </summary>
    IGitHubUserService? UserService { get; }
}
