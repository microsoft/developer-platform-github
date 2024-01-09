// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using System.Net;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Options;
using Octokit.Webhooks;

namespace Microsoft.Developer.Providers.GitHub.API;

/// <summary>
/// Replacement for Octokit.Webhooks.AzureFunctions.GitHubWebhooksHttpFunction that supports new AspNetCore functions
/// https://github.com/octokit/webhooks.net/blob/main/src/Octokit.Webhooks.AzureFunctions/GitHubWebhooksHttpFunction.cs
/// </summary>
public class Webhooks(ILogger<Webhooks> log, IOptions<GitHubOptions> options, WebhookEventProcessor processor)
{
    [Function(nameof(HandleWebhook))]
    public async Task<IActionResult?> HandleWebhook(
        [HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "github/webhooks")] HttpRequest req, CancellationToken token)
    {
        if (!VerifyContentType(req, MediaTypeNames.Application.Json))
        {
            log.LogError("GitHub event does not have the correct content type.");
            return new BadRequestResult();
        }

        var body = await GetBodyAsync(req, token).ConfigureAwait(false);

        if (!VerifySignature(req, options.Value.WebhookSecret, body))
        {
            log.LogError("GitHub event failed signature validation.");
            return new BadRequestResult();
        }

        try
        {
            await processor.ProcessWebhookAsync(req.Headers, body).ConfigureAwait(false);
            return new OkResult();
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Exception processing GitHub event.");
            return new StatusCodeResult((int)HttpStatusCode.InternalServerError);
        }
    }

    private static bool VerifyContentType(HttpRequest req, string expectedContentType)
    {
        if (req.Headers.TryGetValue("Content-Type", out var contentTypes) && contentTypes is [{ } contentType])
        {
            return new ContentType(contentType).MediaType == expectedContentType;
        }

        return false;
    }

    private static async Task<string> GetBodyAsync(HttpRequest req, CancellationToken token)
    {
        using var reader = new StreamReader(req.Body);
        return await reader.ReadToEndAsync(token).ConfigureAwait(false);
    }

    private static bool VerifySignature(HttpRequest req, string? secret, string body)
    {
        var isSignatureExpected = !string.IsNullOrEmpty(secret);

        if (req.Headers.TryGetValue("X-Hub-Signature-256", out var signatures) && signatures is [{ } signature])
        {
            if (!isSignatureExpected)
            {
                return false; // signature wasn't expected, but we got one.
            }

            var keyBytes = Encoding.UTF8.GetBytes(secret!);
            var bodyBytes = Encoding.UTF8.GetBytes(body);

            var hash = HMACSHA256.HashData(keyBytes, bodyBytes);
            var hashHex = Convert.ToHexString(hash);
            var expectedHeader = $"sha256={hashHex.ToLower(CultureInfo.InvariantCulture)}";

            return signature == expectedHeader;
        }
        else if (!isSignatureExpected)
        {
            return true; // signature wasn't expected, nothing to do.
        }
        else
        {
            return false; // signature expected, but we didn't get one.
        }
    }
}