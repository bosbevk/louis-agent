using System.Net;
using System.Security.Claims;
using LouisAgent.Api.Endpoints.Routing;
using LouisAgent.Core.Guardrails.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;

namespace LouisAgent.Api.Tests.Endpoints.Routing;

/// <summary>
/// The gate takes a signed-in person in place of the key (F61.2), but only on a call that carries the browser app's
/// header: a cross-site form sends the cookie too, and can't send the header. Who is signed in is what the accounts'
/// middleware put on the request, played here by a middleware that sets the person the test asks for.
/// </summary>
public class AgentKeyGateSignInTests
{
    private const string Key = "test-key";

    /// <summary>A tiny app: a middleware that names the person (or nobody), the gate, one route under /v1.</summary>
    private static async Task<WebApplication> StartAsync(bool signedIn)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        var app = builder.Build();
        app.Use((context, next) =>
        {
            if (signedIn)
                context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "ann")],
                    "cookie"));
            return next(context);
        });
        app.UseAgentKey(Key);
        app.MapGet("/v1/ping", () => "pong");
        await app.StartAsync();
        return app;
    }

    private static async Task<HttpStatusCode> CallAsync(bool signedIn, string? requestedWith = null,
        string? apiKey = null)
    {
        await using var app = await StartAsync(signedIn);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/v1/ping");
        if (requestedWith is not null) request.Headers.Add(HostAuth.RequestedWithHeader, requestedWith);
        if (apiKey is not null) request.Headers.Add("x-api-key", apiKey);
        using var response = await app.GetTestClient().SendAsync(request);
        await app.StopAsync();
        return response.StatusCode;
    }

    [Test]
    public async Task ASignedInPerson_WithTheHeader_GetsThrough() =>
        Assert.That(await CallAsync(signedIn: true, requestedWith: HostAuth.RequestedWithWeb),
            Is.EqualTo(HttpStatusCode.OK));

    /// <summary>The cookie alone is what a cross-site form would bring, so it isn't enough.</summary>
    [Test]
    public async Task ASignedInPerson_WithoutTheHeader_IsRefused() =>
        Assert.That(await CallAsync(signedIn: true), Is.EqualTo(HttpStatusCode.Unauthorized));

    [Test]
    public async Task TheHeaderAlone_IsRefused() =>
        Assert.That(await CallAsync(signedIn: false, requestedWith: HostAuth.RequestedWithWeb),
            Is.EqualTo(HttpStatusCode.Unauthorized));

    [Test]
    public async Task TheKey_StillGetsThrough_SignedInOrNot() =>
        Assert.That(await CallAsync(signedIn: false, apiKey: Key), Is.EqualTo(HttpStatusCode.OK));
}
