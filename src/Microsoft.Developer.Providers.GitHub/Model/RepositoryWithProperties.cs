// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Octokit;

public class RepositoryWithProperties : Repository
{
    public IDictionary<string, string>? CustomProperties { get; private set; }

#pragma warning disable IDE0290 // Use primary constructor
    public RepositoryWithProperties(string url, string htmlUrl, string cloneUrl, string gitUrl, string sshUrl, string svnUrl, string mirrorUrl, string archiveUrl, long id, string nodeId, User owner, string name, string fullName, bool isTemplate, string description, string homepage, string language, bool @private, bool fork, int forksCount, int stargazersCount, string defaultBranch, int openIssuesCount, DateTimeOffset? pushedAt, DateTimeOffset createdAt, DateTimeOffset updatedAt, RepositoryPermissions permissions, Repository parent, Repository source, LicenseMetadata license, bool hasDiscussions, bool hasIssues, bool hasWiki, bool hasDownloads, bool hasPages, int subscribersCount, long size, bool? allowRebaseMerge, bool? allowSquashMerge, bool? allowMergeCommit, bool archived, int watchersCount, bool? deleteBranchOnMerge, RepositoryVisibility visibility, IEnumerable<string> topics, bool? allowAutoMerge, bool? allowUpdateBranch, bool? webCommitSignoffRequired, IDictionary<string, string>? customProperties)
#pragma warning restore IDE0290 // Use primary constructor

        : base(url, htmlUrl, cloneUrl, gitUrl, sshUrl, svnUrl, mirrorUrl, archiveUrl, id, nodeId, owner, name, fullName, isTemplate, description, homepage, language, @private, fork, forksCount, stargazersCount, defaultBranch, openIssuesCount, pushedAt, createdAt, updatedAt, permissions, parent, source, license, hasDiscussions, hasIssues, hasWiki, hasDownloads, hasPages, subscribersCount, size, allowRebaseMerge, allowSquashMerge, allowMergeCommit, archived, watchersCount, deleteBranchOnMerge, visibility, topics, allowAutoMerge, allowUpdateBranch, webCommitSignoffRequired)
    {
        CustomProperties = customProperties;
    }
}