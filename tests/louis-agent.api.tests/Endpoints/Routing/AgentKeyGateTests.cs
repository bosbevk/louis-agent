using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using LouisAgent.Api.Endpoints;
using LouisAgent.Api.Endpoints.Routing;
using LouisAgent.Choices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;

namespace LouisAgent.Api.Tests.Endpoints.Routing;

/// <summary>
/// The key gate, on the real route table, in memory (CL20 step 2, FX18). A <c>/v1</c> route needs AGENT_API_KEY unless
/// the code marks it Open or AdminKey, so a new route is closed until someone opens it; <c>/health</c> and the web
/// app's files are outside <c>/v1</c>.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0039-api-routes-under-v1.md">ADR 0039: the API routes live under /v1</seealso>
public partial class AgentKeyGateTests
{
    /// <summary>The agent's key the test API is built with.</summary>
    private const string Key = "test-key";

    /// <summary>Calls a route with its first method and sample values, with or without the key.</summary>
    private static async Task<(HttpStatusCode Status, string? ErrorType)> SendAsync(HttpClient client,
        RouteEndpoint route, bool withKey)
    {
        string? method = route.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods.FirstOrDefault();
        using var request = new HttpRequestMessage(new HttpMethod(method ?? "GET"), Sample(ApiTestHost.Pattern(route)));
        if (withKey) request.Headers.Add("x-api-key", Key);
        return await ReadAsync(await client.SendAsync(request));
    }

    /// <summary>The status, and the error's kind when the answer is the API's error JSON.</summary>
    private static async Task<(HttpStatusCode Status, string? ErrorType)> ReadAsync(HttpResponseMessage response)
    {
        string text = await response.Content.ReadAsStringAsync();
        // Not every answer is JSON (a stream, a page), and those have no kind.
        try
        {
            using var json = JsonDocument.Parse(text);
            string? type = json.RootElement.TryGetProperty("error", out var error) &&
                           error.TryGetProperty("type", out var t)
                ? t.GetString()
                : null;
            return (response.StatusCode, type);
        }
        catch (JsonException)
        {
            return (response.StatusCode, null);
        }
    }

    /// <summary>
    /// A pattern with its parameters filled in: <c>{…:int}</c> is 1, <c>{**path}</c> is nope, any other is x.
    /// </summary>
    private static string Sample(string pattern) =>
        Any().Replace(Int().Replace(CatchAll().Replace(pattern, "nope"), "1"), "x");

    /// <summary>A catch-all parameter, <c>{**path}</c>.</summary>
    [GeneratedRegex(@"\{\*\*[^}]+\}")]
    private static partial Regex CatchAll();

    /// <summary>A number parameter, <c>{turn:int}</c>.</summary>
    [GeneratedRegex(@"\{[^}:]+:int\}")]
    private static partial Regex Int();

    /// <summary>Any other parameter, <c>{id}</c>.</summary>
    [GeneratedRegex(@"\{[^}]+\}")]
    private static partial Regex Any();

    /// <summary>Every route that needs the key answers 401 without it, in the API's error shape.</summary>
    [Test]
    public async Task EveryKeyRoute_RefusesACallWithoutTheKey()
    {
        await using var app = ApiTestHost.Build(Key);
        await app.StartAsync();
        var client = app.GetTestClient();

        var refused = new List<string>();
        foreach (var route in ApiTestHost.Routes(app).Where(r => ApiTestHost.Access(r) == RouteAccess.Key))
        {
            var (status, errorType) = await SendAsync(client, route, withKey: false);
            if (status != HttpStatusCode.Unauthorized || errorType != ApiErrorKind.Authentication.Wire())
                refused.Add($"{ApiTestHost.Line(route)} answered {(int)status} {errorType}");
        }

        Assert.That(refused, Is.Empty,
            "A Key route answers 401 authentication_error without the key:\n  " + string.Join("\n  ", refused));
    }

    /// <summary>An open route answers without the key.</summary>
    [Test]
    public async Task TheOpenRoutes_DontAskForTheKey()
    {
        // The gate runs after routing, so it reads the matched route's mark: this is what proves it.
        await using var app = ApiTestHost.Build(Key);
        await app.StartAsync();
        var client = app.GetTestClient();

        var asked = new List<string>();
        foreach (var route in ApiTestHost.Routes(app).Where(r => ApiTestHost.Access(r) == RouteAccess.Open))
        {
            var (status, _) = await SendAsync(client, route, withKey: false);
            if (status == HttpStatusCode.Unauthorized) asked.Add(ApiTestHost.Line(route));
        }

        Assert.That(asked, Is.Empty,
            "An Open route answers without the key (with no price table, /v1/prices is a 404, and that is fine):\n  " +
            string.Join("\n  ", asked));
    }

    /// <summary>The admin routes answer the admin gate, never the agent's, with the agent's key or without.</summary>
    [Test]
    public async Task TheAdminRoutes_AnswerTheAdminGate()
    {
        // The test host sets no admin key, so the admin gate is closed (403): the agent's key gate never answers on
        // these routes.
        await using var app = ApiTestHost.Build(Key);
        await app.StartAsync();
        var client = app.GetTestClient();

        foreach (bool withKey in new[] { false, true })
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/v1/admin/benchmarks");
            if (withKey) request.Headers.Add("x-api-key", Key);
            var (status, errorType) = await ReadAsync(await client.SendAsync(request));

            Assert.That((status, errorType),
                Is.EqualTo((HttpStatusCode.Forbidden, ApiErrorKind.Permission.Wire())),
                $"with the agent's key: {withKey}");
        }
    }

    /// <summary>
    /// A <c>/v1</c> path no route matches is the API's JSON 404 with the key, and the gate's 401 without.
    /// </summary>
    [Test]
    public async Task AnUnknownV1Path_IsAJson404_WithTheKey_AndA401Without()
    {
        await using var app = ApiTestHost.Build(Key);
        // As Program.cs maps the web app: its fallback and /v1's are both fallbacks of the same order, so only route
        // precedence keeps the JSON 404 in front of the web app's page, and this test is what holds that.
        app.MapFallbackToFile("index.html");
        await app.StartAsync();
        var client = app.GetTestClient();

        using var withKey = new HttpRequestMessage(HttpMethod.Get, "/v1/nope");
        withKey.Headers.Add("x-api-key", Key);
        var known = await ReadAsync(await client.SendAsync(withKey));
        var without = await ReadAsync(await client.GetAsync("/v1/nope"));

        Assert.That(known, Is.EqualTo((HttpStatusCode.NotFound, ApiErrorKind.NotFound.Wire())),
            "never the web app's page");
        Assert.That(without, Is.EqualTo((HttpStatusCode.Unauthorized, ApiErrorKind.Authentication.Wire())));
    }

    /// <summary>With the key, a route that needs it answers.</summary>
    [Test]
    public async Task TheKey_LetsAKeyRouteThrough()
    {
        await using var app = ApiTestHost.Build(Key);
        await app.StartAsync();
        var client = app.GetTestClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/v1/toolsets");
        request.Headers.Add("x-api-key", Key);
        using var response = await client.SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
}
