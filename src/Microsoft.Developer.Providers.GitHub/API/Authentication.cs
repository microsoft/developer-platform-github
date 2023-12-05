// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Developer.Features;

namespace Microsoft.Developer.Providers.GitHub.API;

public class Authentication(IGitHubAppService gitHubAppService, IUserOAuthLoginManager logins)
{
    [Authorize]
    [Function(nameof(Login))]
    public async Task<IActionResult> Login(
        [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "auth/login")] HttpRequest req,
        FunctionContext context,
        CancellationToken token)
    {
        if (GetQueryValue(req, "redirect_uri") is not { } redirectUri)
        {
            return new BadRequestObjectResult(new ProblemDetails { Detail = "Query parameter 'redirect_uri' is required" });
        }

        var user = context.Features.GetRequiredFeature<IDeveloperPlatformUserFeature>().User;
        var callbackUri = req.HttpContext.GetAbsoluteUri("auth/callback");

        var state = await logins.AddAsync(user, redirectUri, token);
        var url = await gitHubAppService.GetGitHubLoginUrl(callbackUri, state, token);

        return new OkObjectResult(new { Uri = url.AbsoluteUri });
    }

    [Function(nameof(Callback))]
    public async Task<IActionResult> Callback(
        [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "auth/callback")] HttpRequest req,
        CancellationToken token)
    {
        if (GetQueryValue(req, "code") is not { } code)
        {
            return new BadRequestObjectResult(new ProblemDetails { Detail = "Query parameter 'code' is required" });
        }

        if (GetQueryValue(req, "state") is not { } state)
        {
            return new BadRequestObjectResult(new ProblemDetails { Detail = "Query parameter 'state' is required" });
        }

        var redirectUri = await logins.RegisterAsync(code, state, token);

        if (redirectUri is null)
        {
            return new BadRequestObjectResult(new ProblemDetails { Detail = "Invalid state" });
        }
        else
        {
            return new RedirectResult(redirectUri);
        }
    }

    private static string? GetQueryValue(HttpRequest req, string queryName)
    {
        if (req.Query.TryGetValue(queryName, out var result) && result is [{ } queryValue])
        {
            return queryValue;
        }

        return null;
    }
}
