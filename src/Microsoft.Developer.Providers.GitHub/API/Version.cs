// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Microsoft.Developer.Providers.GitHub.API;

public class Version
{
    [Function(nameof(Version))]
    public IActionResult Get([HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "version")] HttpRequest req)
        => new OkObjectResult(Environment.GetEnvironmentVariable("DEVELOPER_API_IMAGE_VERSION") is { } version ? version : "OK");
}
