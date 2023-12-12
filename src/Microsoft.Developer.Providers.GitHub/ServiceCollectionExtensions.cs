// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using Microsoft.Developer.Data;
using Microsoft.Developer.Data.Cosmos;
using Microsoft.Developer.Providers.GitHub.Webhooks;
using Microsoft.Developer.Serialization.Json.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Octokit.Webhooks;

namespace Microsoft.Developer.Providers.GitHub;

public static class ServiceCollectionExtensions
{
    public static IDeveloperPlatformBuilder AddCosmos(this IDeveloperPlatformBuilder builder, IConfiguration config, bool removeTrace = true)
    {
        builder.Services
            .AddSingleton(typeof(IDocumentRepositoryFactory<>), typeof(CosmosDocumentRepositoryFactory<>));

        builder.Services
            .Configure<CosmosOptions>(config.GetSection(CosmosOptions.Section));

        builder.AddDocumentRepository<MappedUser>(nameof(MappedUser), options =>
        {
            options.DatabaseName = "GitHub";
            options.ContainerName = "Users";
            options.UniqueKeys.Add("/localUser/id");
            options.UniqueKeys.Add("/localUser/login");
            options.SerializerOptions = EntitySerializerOptions.Database;
        });

        builder.Services.AddSingleton<IMappedUserRepository<MappedUser, GitHubUser>, MappedUserRepository<MappedUser, GitHubUser>>(
            services => new MappedUserRepository<MappedUser, GitHubUser>(
                services.GetRequiredService<IDocumentRepositoryFactory<MappedUser>>().Create(nameof(MappedUser))));

        if (removeTrace)
        {
            var defaultTrace = Type.GetType("Microsoft.Azure.Cosmos.Core.Trace.DefaultTrace,Microsoft.Azure.Cosmos.Direct");
            var traceSource = (TraceSource?)defaultTrace?.GetProperty("TraceSource")?.GetValue(null);
            traceSource?.Listeners.Remove("Default");
        }

        return builder;
    }

    public static IDeveloperPlatformBuilder AddGitHubProvider(this IDeveloperPlatformBuilder builder, Action<IGitHubProviderBuilder> configure)
    {
        builder.Services.TryAddSingleton(TimeProvider.System);

        builder.Services
            .AddSingleton<IGitHubAppService, GitHubAppService>()
            .AddSingleton<OAuthUserLoginManager>()
            .AddSingleton<ILocalUserManager<MappedUser, GitHubUser>>(ctx => ctx.GetRequiredService<OAuthUserLoginManager>())
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
}
