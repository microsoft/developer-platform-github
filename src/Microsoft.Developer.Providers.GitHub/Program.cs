// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Developer.Azure;
using Microsoft.Developer.Entities.Serialization;
using Microsoft.Developer.Hosting.Middleware;
using Microsoft.Developer.Providers.GitHub;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication((_, builder) =>
    {
        builder
            .UseDeveloperPlatform(excludingFunctions: nameof(Version))
            .UseWhen<DeveloperPlatformGitHubMiddleware>(context => context.IsHttpTrigger());
    })
    .ConfigureAppConfiguration((context, builder) =>
    {
        context.HostingEnvironment
            .SetApplicationNameFromAssembly();

        builder
            .AddMsDeveloperConfiguration(builder.Build(), context.HostingEnvironment);
    })
    .ConfigureServices((context, services) =>
    {
        services
            .AddAuthentication()
            .AddMicrosoftIdentityWebApi(jwtOptions => { }, identityOptions =>
            {
                context.Configuration.GetSection(AzureAdOptions.Section).Bind(identityOptions);
            })
            .EnableTokenAcquisitionToCallDownstreamApi(clientOptions =>
            {
                context.Configuration.GetSection(AzureAdOptions.Section).Bind(clientOptions);
            })
            .AddDistributedTokenCaches();

        services.AddDeveloperPlatform()
            .AddEntitySerialization()
            .AddAzure(context.Configuration)
            .AddCosmos(context.Configuration)
            .AddGitHubProvider(builder => builder
                .AddOptions(context.Configuration)
                .AddWebhooks(context.Configuration)
            );
    })
    .Build();

host.Run();
