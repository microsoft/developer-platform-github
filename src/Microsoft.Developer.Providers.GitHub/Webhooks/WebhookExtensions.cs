// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Octokit.Webhooks;

namespace Microsoft.Developer.Providers.GitHub.Webhooks;

public static class WebhookExtensions
{
    public static long? GetInstallationId<T>(this T hook, WebhookHeaders? headers = null)
        where T : WebhookEvent
    {
        // TODO: GithubAppAuthorizationRevokedEvent does not have an Installation property

        // WebhookEvent (base type for all Events) has a property 'Installation' of type InstallationLite
        // Some derived classes of WebhookEvent (ex: InstalltionEvent) have a property Installation of type
        // Installation that hides the base type's Installation property (WebhookEvent.Installation),
        // so calling hook.Installation.Id in generic methods returns null even though DerivedType.Installation
        // has a value. To work around this, we use reflection to get the value of the Installation property
        var installation = hook
            .GetType()
            .GetProperties()
            .FirstOrDefault(p => p.Name.Equals("Installation") && p.GetValue(hook) is not null)?
            .GetValue(hook) as Octokit.Webhooks.Models.Installation;

        return installation?.Id ?? hook.Installation?.Id;
    }
}

