// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Developer.Providers.GitHub.Model;
using Microsoft.Developer.Providers.GitHub.Webhooks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Octokit.Webhooks;

namespace Microsoft.Developer.Providers.GitHub;

public static class ServiceCollectionExtensions
{
    public static IDeveloperPlatformBuilder AddGitHubProvider(this IDeveloperPlatformBuilder builder, Action<IGitHubProviderBuilder> configure)
    {
        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services
            .AddSingleton<IGitHubAppService, GitHubAppService>()
            .AddSingleton<OAuthUserLoginManager>()
            .AddSingleton<ILocalUserManager<GitHubUser>>(ctx => ctx.GetRequiredService<OAuthUserLoginManager>())
            .AddSingleton<ILocalUserManager>(ctx => ctx.GetRequiredService<OAuthUserLoginManager>())
            .AddSingleton<IUserOAuthLoginManager>(ctx => ctx.GetRequiredService<OAuthUserLoginManager>());

        builder.Services
            .AddScoped<IGitHubInstallationService, GitHubInstallationService>();

        builder.AddProvider(b => new GhBuilder(b, b.Services), configure);

        return builder;
    }

    private sealed record GhBuilder(IDeveloperPlatformBuilder Builder, IServiceCollection Services) : IGitHubProviderBuilder;

    public static IGitHubProviderBuilder AddOptions(this IGitHubProviderBuilder builder, IConfiguration configuration)
    {
        builder.Services
            .Configure<GitHubOptions>(configuration.GetSection(GitHubOptions.Section));

        return builder;
    }

    public static IGitHubProviderBuilder AddWebhooks(this IGitHubProviderBuilder builder, IConfiguration configuration)
    {
        builder.Services
            .AddSingleton<WebhookEventProcessor, GitHubWebhookProcessor>();

        return builder;
    }

    public static IGitHubProviderBuilder AddUserDatabase(this IGitHubProviderBuilder services, Action<DbContextOptionsBuilder> configure)
    {
        services.Services.AddHostedService<EnsureDbStartup<GitHubDbContext>>();
        services.Services.AddDbContextFactory<GitHubDbContext>(options =>
        {
            configure(options);
        });

        return services;
    }

    private sealed class EnsureDbStartup<TContext>(IDbContextFactory<TContext> factory, ILogger<EnsureDbStartup<TContext>> logger) : BackgroundService
        where TContext : DbContext
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var context = factory.CreateDbContext();

            logger.LogDebug("Ensuring {Context} is available", typeof(TContext).Name);

            if (await context.Database.EnsureCreatedAsync(stoppingToken))
            {
                logger.LogInformation("{Context} was created", typeof(TContext).Name);
            }
        }
    }
}
