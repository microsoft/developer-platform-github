// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Developer.Entities;
using Microsoft.DurableTask.Client;

namespace Microsoft.Developer.Providers.GitHub.API;

[RequireLocalUser(Realm = "GitHub")]
[Authorize]
public class GetStatus
{
    [Function(nameof(GetStatus))]
    public async Task<IActionResult> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "status/{instanceId}")] HttpRequest req, string instanceId,
        [DurableClient] DurableTaskClient client, FunctionContext context, CancellationToken token)
    {
        var log = context.GetLogger<GetStatus>();

        log.LogInformation("Checking instance: {instanceId}", instanceId);

        if (await client.GetInstanceAsync(instanceId, true, token) is { } instance)
        {
            if (instance.IsCompleted)
            {
                // TODO: what to return here?
                // var location = req.HttpContext.GetAbsoluteUri($"entities");
                // return new RedirectResult(location.AbsoluteUri, true);
                return new TemplateResult(instance.ReadOutputAs<EntityRef[]>() ?? []);
            }
            else
            {
                return new AcceptedResult(req.HttpContext.GetStatusUri(instanceId), null);
            }
        }
        else
        {
            log.LogWarning("Instance not found: {instanceId}", instanceId);
            return new NotFoundResult();
        }
    }
}
