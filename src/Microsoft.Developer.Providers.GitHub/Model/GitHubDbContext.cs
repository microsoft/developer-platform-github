// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.EntityFrameworkCore;

namespace Microsoft.Developer.Providers.GitHub.Model;

public class GitHubDbContext(DbContextOptions<GitHubDbContext> options) : DbContext(options)
{
    public DbSet<MappedUser> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<MappedUser>()
            .ToContainer("Users")
            .HasPartitionKey(u => u.Tenant)
            .HasNoDiscriminator()
            .HasKey(u => u.Id);
    }
}
