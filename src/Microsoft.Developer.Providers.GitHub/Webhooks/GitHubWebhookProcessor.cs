// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Octokit.Webhooks;
using Octokit.Webhooks.Events;
using Octokit.Webhooks.Events.BranchProtectionRule;
using Octokit.Webhooks.Events.CheckRun;
using Octokit.Webhooks.Events.CheckSuite;
using Octokit.Webhooks.Events.CodeScanningAlert;
using Octokit.Webhooks.Events.CommitComment;
using Octokit.Webhooks.Events.ContentReference;
using Octokit.Webhooks.Events.CustomProperty;
using Octokit.Webhooks.Events.CustomPropertyValues;
using Octokit.Webhooks.Events.DependabotAlert;
using Octokit.Webhooks.Events.DeployKey;
using Octokit.Webhooks.Events.Deployment;
using Octokit.Webhooks.Events.DeploymentProtectionRule;
using Octokit.Webhooks.Events.DeploymentReview;
using Octokit.Webhooks.Events.DeploymentStatus;
using Octokit.Webhooks.Events.Discussion;
using Octokit.Webhooks.Events.DiscussionComment;
using Octokit.Webhooks.Events.GithubAppAuthorization;
using Octokit.Webhooks.Events.Installation;
using Octokit.Webhooks.Events.InstallationRepositories;
using Octokit.Webhooks.Events.InstallationTarget;
using Octokit.Webhooks.Events.IssueComment;
using Octokit.Webhooks.Events.Issues;
using Octokit.Webhooks.Events.Label;
using Octokit.Webhooks.Events.MarketplacePurchase;
using Octokit.Webhooks.Events.Member;
using Octokit.Webhooks.Events.Membership;
using Octokit.Webhooks.Events.MergeGroup;
using Octokit.Webhooks.Events.MergeQueueEntry;
using Octokit.Webhooks.Events.Meta;
using Octokit.Webhooks.Events.Milestone;
using Octokit.Webhooks.Events.Organization;
using Octokit.Webhooks.Events.OrgBlock;
using Octokit.Webhooks.Events.Package;
using Octokit.Webhooks.Events.Project;
using Octokit.Webhooks.Events.ProjectCard;
using Octokit.Webhooks.Events.ProjectColumn;
using Octokit.Webhooks.Events.PullRequest;
using Octokit.Webhooks.Events.PullRequestReview;
using Octokit.Webhooks.Events.PullRequestReviewComment;
using Octokit.Webhooks.Events.PullRequestReviewThread;
using Octokit.Webhooks.Events.RegistryPackage;
using Octokit.Webhooks.Events.Release;
using Octokit.Webhooks.Events.Repository;
using Octokit.Webhooks.Events.RepositoryDispatch;
using Octokit.Webhooks.Events.RepositoryRuleset;
using Octokit.Webhooks.Events.RepositoryVulnerabilityAlert;
using Octokit.Webhooks.Events.SecretScanningAlert;
using Octokit.Webhooks.Events.SecretScanningAlertLocation;
using Octokit.Webhooks.Events.SecurityAdvisory;
using Octokit.Webhooks.Events.Sponsorship;
using Octokit.Webhooks.Events.Star;
using Octokit.Webhooks.Events.Team;
using Octokit.Webhooks.Events.Watch;
using Octokit.Webhooks.Events.WorkflowJob;
using Octokit.Webhooks.Events.WorkflowRun;
using Octokit.Webhooks.Models;

namespace Microsoft.Developer.Providers.GitHub.Webhooks;

public class GitHubWebhookProcessor(ILogger<GitHubWebhookProcessor> log, IUserOAuthLoginManager logins) : WebhookEventProcessor
{
    protected override Task ProcessBranchProtectionRuleWebhookAsync(WebhookHeaders headers, BranchProtectionRuleEvent hookEvent, BranchProtectionRuleAction action) => HandleUnhandledWebhook(headers, hookEvent, action);
    protected override Task ProcessCheckRunWebhookAsync(WebhookHeaders headers, CheckRunEvent hookEvent, CheckRunAction action) => HandleUnhandledWebhook(headers, hookEvent, action);
    protected override Task ProcessCheckSuiteWebhookAsync(WebhookHeaders headers, CheckSuiteEvent hookEvent, CheckSuiteAction action) => HandleUnhandledWebhook(headers, hookEvent, action);
    protected override Task ProcessCodeScanningAlertWebhookAsync(WebhookHeaders headers, CodeScanningAlertEvent hookEvent, CodeScanningAlertAction action) => HandleUnhandledWebhook(headers, hookEvent, action);
    protected override Task ProcessCommitCommentWebhookAsync(WebhookHeaders headers, CommitCommentEvent hookEvent, CommitCommentAction action) => HandleUnhandledWebhook(headers, hookEvent, action);
    protected override Task ProcessContentReferenceWebhookAsync(WebhookHeaders headers, ContentReferenceEvent hookEvent, ContentReferenceAction action) => HandleUnhandledWebhook(headers, hookEvent, action);
    protected override Task ProcessCreateWebhookAsync(WebhookHeaders headers, CreateEvent hookEvent) => HandleUnhandledWebhook(headers, hookEvent);
    protected override Task ProcessCustomPropertyValuesWebhookAsync(WebhookHeaders headers, CustomPropertyValuesEvent hookEvent, CustomPropertyValuesAction action) => HandleUnhandledWebhook(headers, hookEvent, action);
    protected override Task ProcessCustomPropertyWebhookAsync(WebhookHeaders headers, CustomPropertyEvent hookEvent, CustomPropertyAction action) => HandleUnhandledWebhook(headers, hookEvent, action);
    protected override Task ProcessDeleteWebhookAsync(WebhookHeaders headers, DeleteEvent hookEvent) => HandleUnhandledWebhook(headers, hookEvent);
    protected override Task ProcessDependabotAlertWebhookAsync(WebhookHeaders headers, DependabotAlertEvent hookEvent, DependabotAlertAction action) => HandleUnhandledWebhook(headers, hookEvent, action);
    protected override Task ProcessDeployKeyWebhookAsync(WebhookHeaders headers, DeployKeyEvent hookEvent, DeployKeyAction action) => HandleUnhandledWebhook(headers, hookEvent, action);
    protected override Task ProcessDeploymentReviewWebhookAsync(WebhookHeaders headers, DeploymentReviewEvent hookEvent, DeploymentReviewAction action) => HandleUnhandledWebhook(headers, hookEvent, action);
    protected override Task ProcessDeploymentStatusWebhookAsync(WebhookHeaders headers, DeploymentStatusEvent hookEvent, DeploymentStatusAction action) => HandleUnhandledWebhook(headers, hookEvent, action);
    protected override Task ProcessDeploymentWebhookAsync(WebhookHeaders headers, DeploymentEvent hookEvent, DeploymentAction action) => HandleUnhandledWebhook(headers, hookEvent, action);
    protected override Task ProcessDeployProtectionRuleWebhookAsync(WebhookHeaders headers, DeploymentProtectionRuleEvent hookEvent, DeploymentProtectionRuleAction action) => HandleUnhandledWebhook(headers, hookEvent, action);
    protected override Task ProcessDiscussionCommentWebhookAsync(WebhookHeaders headers, DiscussionCommentEvent hookEvent, DiscussionCommentAction action) => HandleUnhandledWebhook(headers, hookEvent, action);
    protected override Task ProcessDiscussionWebhookAsync(WebhookHeaders headers, DiscussionEvent hookEvent, DiscussionAction action) => HandleUnhandledWebhook(headers, hookEvent, action);
    protected override Task ProcessForkWebhookAsync(WebhookHeaders headers, ForkEvent hookEvent) => HandleUnhandledWebhook(headers, hookEvent);
    protected override async Task ProcessGithubAppAuthorizationWebhookAsync(WebhookHeaders headers, GithubAppAuthorizationEvent hookEvent, GithubAppAuthorizationAction action)
    {
        LogWebhook(headers, hookEvent, action);

        if (action == GithubAppAuthorizationAction.Revoked && hookEvent.Sender is User sender)
        {
            log.LogInformation("Removing mapped user for {Login}", sender.Login);
            await logins.RemoveAsync(new GitHubUser { Id = sender.Id.ToString(), Name = sender.Name, Login = sender.Login }, CancellationToken.None);
        }
    }

    protected override Task ProcessGollumWebhookAsync(WebhookHeaders headers, GollumEvent hookEvent) => HandleUnhandledWebhook(headers, hookEvent);
    protected override Task ProcessInstallationRepositoriesWebhookAsync(WebhookHeaders headers, InstallationRepositoriesEvent hookEvent, InstallationRepositoriesAction action) //  => HandleUnhandledWebhook(headers, hookEvent, action);
    {
        LogWebhook(headers, hookEvent, action);
        return Task.CompletedTask;
    }

    protected override Task ProcessInstallationTargetWebhookAsync(WebhookHeaders headers, InstallationTargetEvent hookEvent, InstallationTargetAction action) //  => HandleUnhandledWebhook(headers, hookEvent, action);
    {
        LogWebhook(headers, hookEvent, action);
        return Task.CompletedTask;
    }

    protected override Task ProcessInstallationWebhookAsync(WebhookHeaders headers, InstallationEvent hookEvent, InstallationAction action) //  => HandleUnhandledWebhook(headers, hookEvent, action);
    {
        LogWebhook(headers, hookEvent, action);
        return Task.CompletedTask;
        // _ = await github
        //     .SaveInstallation(hookEvent.Installation)
        //     .ConfigureAwait(false);
        // return Task.CompletedTask;
    }

    protected override Task ProcessIssueCommentWebhookAsync(WebhookHeaders headers, IssueCommentEvent hookEvent, IssueCommentAction action) => HandleUnhandledWebhook(headers, hookEvent, action);
    protected override Task ProcessIssuesWebhookAsync(WebhookHeaders headers, IssuesEvent hookEvent, IssuesAction action) => HandleUnhandledWebhook(headers, hookEvent, action);
    protected override Task ProcessLabelWebhookAsync(WebhookHeaders headers, LabelEvent hookEvent, LabelAction action) => HandleUnhandledWebhook(headers, hookEvent, action);
    protected override Task ProcessMarketplacePurchaseWebhookAsync(WebhookHeaders headers, MarketplacePurchaseEvent hookEvent, MarketplacePurchaseAction action) => HandleUnhandledWebhook(headers, hookEvent, action);
    protected override Task ProcessMembershipWebhookAsync(WebhookHeaders headers, MembershipEvent hookEvent, MembershipAction action) => HandleUnhandledWebhook(headers, hookEvent, action);
    protected override Task ProcessMemberWebhookAsync(WebhookHeaders headers, MemberEvent hookEvent, MemberAction action) => HandleUnhandledWebhook(headers, hookEvent, action);
    protected override Task ProcessMergeGroupWebhookAsync(WebhookHeaders headers, MergeGroupEvent hookEvent, MergeGroupAction action) => HandleUnhandledWebhook(headers, hookEvent, action);
    protected override Task ProcessMergeQueueEntryWebhookAsync(WebhookHeaders headers, MergeQueueEntryEvent hookEvent, MergeQueueEntryAction action) => HandleUnhandledWebhook(headers, hookEvent, action);
    protected override Task ProcessMetaWebhookAsync(WebhookHeaders headers, MetaEvent hookEvent, MetaAction action) => HandleUnhandledWebhook(headers, hookEvent, action);
    protected override Task ProcessMilestoneWebhookAsync(WebhookHeaders headers, MilestoneEvent hookEvent, MilestoneAction action) => HandleUnhandledWebhook(headers, hookEvent, action);
    protected override Task ProcessOrganizationWebhookAsync(WebhookHeaders headers, OrganizationEvent hookEvent, OrganizationAction action) => HandleUnhandledWebhook(headers, hookEvent, action);
    protected override Task ProcessOrgBlockWebhookAsync(WebhookHeaders headers, OrgBlockEvent hookEvent, OrgBlockAction action) => HandleUnhandledWebhook(headers, hookEvent, action);
    protected override Task ProcessPackageWebhookAsync(WebhookHeaders headers, PackageEvent hookEvent, PackageAction action) => HandleUnhandledWebhook(headers, hookEvent, action);
    protected override Task ProcessPageBuildWebhookAsync(WebhookHeaders headers, PageBuildEvent hookEvent) => HandleUnhandledWebhook(headers, hookEvent);
    protected override Task ProcessPingWebhookAsync(WebhookHeaders headers, PingEvent hookEvent) => HandleUnhandledWebhook(headers, hookEvent);
    // protected override async Task ProcessPingWebhookAsync(WebhookHeaders headers, PingEvent hookEvent)
    // {
    //     LogWebhook(headers, hookEvent);

    //     await github.GetRepositoriesAsync();
    // }
    protected override Task ProcessProjectCardWebhookAsync(WebhookHeaders headers, ProjectCardEvent hookEvent, ProjectCardAction action) => HandleUnhandledWebhook(headers, hookEvent, action);
    protected override Task ProcessProjectColumnWebhookAsync(WebhookHeaders headers, ProjectColumnEvent hookEvent, ProjectColumnAction action) => HandleUnhandledWebhook(headers, hookEvent, action);
    protected override Task ProcessProjectWebhookAsync(WebhookHeaders headers, ProjectEvent hookEvent, ProjectAction action) => HandleUnhandledWebhook(headers, hookEvent, action);
    protected override Task ProcessPublicWebhookAsync(WebhookHeaders headers, PublicEvent hookEvent) => HandleUnhandledWebhook(headers, hookEvent);
    protected override Task ProcessPullRequestReviewCommentWebhookAsync(WebhookHeaders headers, PullRequestReviewCommentEvent hookEvent, PullRequestReviewCommentAction action) => HandleUnhandledWebhook(headers, hookEvent, action);
    protected override Task ProcessPullRequestReviewThreadWebhookAsync(WebhookHeaders headers, PullRequestReviewThreadEvent hookEvent, PullRequestReviewThreadAction action) => HandleUnhandledWebhook(headers, hookEvent, action);
    protected override Task ProcessPullRequestReviewWebhookAsync(WebhookHeaders headers, PullRequestReviewEvent hookEvent, PullRequestReviewAction action) => HandleUnhandledWebhook(headers, hookEvent, action);
    protected override Task ProcessPullRequestWebhookAsync(WebhookHeaders headers, PullRequestEvent hookEvent, PullRequestAction action) => HandleUnhandledWebhook(headers, hookEvent, action);
    protected override Task ProcessPushWebhookAsync(WebhookHeaders headers, PushEvent hookEvent) => HandleUnhandledWebhook(headers, hookEvent);
    protected override Task ProcessRegistryPackageWebhookAsync(WebhookHeaders headers, RegistryPackageEvent hookEvent, RegistryPackageAction action) => HandleUnhandledWebhook(headers, hookEvent, action);
    protected override Task ProcessReleaseWebhookAsync(WebhookHeaders headers, ReleaseEvent hookEvent, ReleaseAction action) => HandleUnhandledWebhook(headers, hookEvent, action);
    protected override Task ProcessRepositoryDispatchWebhookAsync(WebhookHeaders headers, RepositoryDispatchEvent hookEvent, RepositoryDispatchAction action) => HandleUnhandledWebhook(headers, hookEvent, action);
    protected override Task ProcessRepositoryImportWebhookAsync(WebhookHeaders headers, RepositoryImportEvent hookEvent) => HandleUnhandledWebhook(headers, hookEvent);
    protected override Task ProcessRepositoryRulesetWebhookAsync(WebhookHeaders headers, RepositoryRulesetEvent hookEvent, RepositoryRulesetAction action) => HandleUnhandledWebhook(headers, hookEvent, action);
    protected override Task ProcessRepositoryVulnerabilityAlertWebhookAsync(WebhookHeaders headers, RepositoryVulnerabilityAlertEvent hookEvent, RepositoryVulnerabilityAlertAction action) => HandleUnhandledWebhook(headers, hookEvent, action);
    protected override Task ProcessRepositoryWebhookAsync(WebhookHeaders headers, RepositoryEvent hookEvent, RepositoryAction action) => HandleUnhandledWebhook(headers, hookEvent, action);
    protected override Task ProcessSecretScanningAlertLocationWebhookAsync(WebhookHeaders headers, SecretScanningAlertLocationEvent hookEvent, SecretScanningAlertLocationAction action) => HandleUnhandledWebhook(headers, hookEvent, action);
    protected override Task ProcessSecretScanningAlertWebhookAsync(WebhookHeaders headers, SecretScanningAlertEvent hookEvent, SecretScanningAlertAction action) => HandleUnhandledWebhook(headers, hookEvent, action);
    protected override Task ProcessSecurityAdvisoryWebhookAsync(WebhookHeaders headers, SecurityAdvisoryEvent hookEvent, SecurityAdvisoryAction action) => HandleUnhandledWebhook(headers, hookEvent, action);
    protected override Task ProcessSponsorshipWebhookAsync(WebhookHeaders headers, SponsorshipEvent hookEvent, SponsorshipAction action) => HandleUnhandledWebhook(headers, hookEvent, action);
    protected override Task ProcessStarWebhookAsync(WebhookHeaders headers, StarEvent hookEvent, StarAction action) => HandleUnhandledWebhook(headers, hookEvent, action);
    protected override Task ProcessStatusWebhookAsync(WebhookHeaders headers, StatusEvent hookEvent) => HandleUnhandledWebhook(headers, hookEvent);
    protected override Task ProcessTeamAddWebhookAsync(WebhookHeaders headers, TeamAddEvent hookEvent) => HandleUnhandledWebhook(headers, hookEvent);
    protected override Task ProcessTeamWebhookAsync(WebhookHeaders headers, TeamEvent hookEvent, TeamAction action) => HandleUnhandledWebhook(headers, hookEvent, action);
    protected override Task ProcessWatchWebhookAsync(WebhookHeaders headers, WatchEvent hookEvent, WatchAction action) => HandleUnhandledWebhook(headers, hookEvent, action);
    protected override Task ProcessWorkflowDispatchWebhookAsync(WebhookHeaders headers, WorkflowDispatchEvent hookEvent) => HandleUnhandledWebhook(headers, hookEvent);
    protected override Task ProcessWorkflowJobWebhookAsync(WebhookHeaders headers, WorkflowJobEvent hookEvent, WorkflowJobAction action) => HandleUnhandledWebhook(headers, hookEvent, action);
    protected override Task ProcessWorkflowRunWebhookAsync(WebhookHeaders headers, WorkflowRunEvent hookEvent, WorkflowRunAction action) => HandleUnhandledWebhook(headers, hookEvent, action);

    private void LogWebhook<TEvent>(WebhookHeaders headers, TEvent hookEvent)
        where TEvent : WebhookEvent
    => log.LogInformation("Received GitHub Webhook: [ Event: {event}, Action: {action}, Installation: {installation} ]", headers.Event, hookEvent.Action ?? "null", hookEvent.GetInstallationId()?.ToString() ?? "null");

    private void LogWebhook<TEvent, TAction>(WebhookHeaders headers, TEvent hookEvent, TAction action)
        where TEvent : WebhookEvent
        where TAction : WebhookEventAction
    => log.LogInformation("Received GitHub Webhook: [ Event: {event}, Action: {action}, Installation: {installation} ]", headers.Event, action ?? hookEvent.Action ?? "null", hookEvent.GetInstallationId()?.ToString() ?? "null");

    private Task HandleUnhandledWebhook<TEvent>(WebhookHeaders headers, TEvent hookEvent)
        where TEvent : WebhookEvent
    {
        LogWebhook(headers, hookEvent);
        return Task.CompletedTask;
    }

    private Task HandleUnhandledWebhook<TEvent, TAction>(WebhookHeaders headers, TEvent hookEvent, TAction action)
        where TEvent : WebhookEvent
        where TAction : WebhookEventAction
    {
        LogWebhook(headers, hookEvent, action);
        return Task.CompletedTask;
    }
}
