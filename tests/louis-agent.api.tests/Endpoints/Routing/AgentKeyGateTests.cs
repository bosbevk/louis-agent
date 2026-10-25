using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using LouisAgent.Api.Endpoints.Routing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;

namespace LouisAgent.Api.Tests.Endpoints.Routing;

// CL20 step 2 (ADR 0039, rule 6; FX18): the key gate, on the real table, in memory. A /v1 route needs AGENT_API_KEY unless the code
// marks it Open or AdminKey, so a new route is closed until someone opens it; /health and the web app's files are outside /v1.
public partial class AgentKeyGateTests
{
    private const string Key = "test-key";

    private static async Task<(HttpStatusCode Status, string? ErrorType)> SendAsync(HttpClient client, RouteEndpoint route, bool withKey)
    {
        string? method = route.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods.FirstOrDefault();
        using var request = new HttpRequestMessage(new HttpMethod(method ?? "GET"), Sample(ApiTestHost.Pattern(route)));
        if (withKey) request.Headers.Add("x-api-key", Key);
        return await ReadAsync(await client.SendAsync(request));
    }

    private static async Task<(HttpStatusCode Status, string? ErrorType)> ReadAsync(HttpResponseMessage response)
    {
        string text = await response.Content.ReadAsStringAsync();
        try
        {
            using var json = JsonDocument.Parse(text);
            string? type = json.RootElement.TryGetProperty("error", out var error) && error.TryGetProperty("type", out var t) ? t.GetString() : null;
            return (response.StatusCode, type);
        }
        catch (JsonException)
        {
            return (response.StatusCode, null);
        }
    }

    /// <summary>A pattern with its parameters filled in: <c>{…:int}</c> is 1, <c>{**path}</c> is nope, any other is x.</summary>
    private static string Sample(string pattern) =>
        Any().Replace(Int().Replace(CatchAll().Replace(pattern, "nope"), "1"), "x");

    [GeneratedRegex(@"\{\*\*[^}]+\}")] private static partial Regex CatchAll();
    [GeneratedRegex(@"\{[^}:]+:int\}")] private static partial Regex Int();
    [GeneratedRegex(@"\{[^}]+\}")] private static partial Regex Any();

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
            if (status != HttpStatusCode.Unauthorized || errorType != "authentication_error") refused.Add($"{ApiTestHost.Line(route)} answered {(int)status} {errorType}");
        }

        Assert.That(refused, Is.Empty, "A Key route answers 401 authentication_error without the key:\n  " + string.Join("\n  ", refused));
    }

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

        Assert.That(asked, Is.Empty, "An Open route answers without the key (with no price table, /v1/prices is a 404, and that is fine):\n  " + string.Join("\n  ", asked));
    }

    [Test]
    public async Task TheAdminRoutes_AnswerTheAdminGate()
    {
        // The test host sets no admin key, so the admin gate is closed (403): the agent's key gate never answers on these routes.
        await using var app = ApiTestHost.Build(Key);
        await app.StartAsync();
        var client = app.GetTestClient();

        foreach (bool withKey in new[] { false, true })
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/v1/admin/benchmarks");
            if (withKey) request.Headers.Add("x-api-key", Key);
            var (status, errorType) = await ReadAsync(await client.SendAsync(request));

            Assert.That((status, errorType), Is.EqualTo((HttpStatusCode.Forbidden, "permission_error")), $"with the agent's key: {withKey}");
        }
    }

    [Test]
    public async Task AnUnknownV1Path_IsAJson404_WithTheKey_AndA401Without()
    {
        await using var app = ApiTestHost.Build(Key);
        await app.StartAsync();
        var client = app.GetTestClient();

        using var withKey = new HttpRequestMessage(HttpMethod.Get, "/v1/nope");
        withKey.Headers.Add("x-api-key", Key);
        var known = await ReadAsync(await client.SendAsync(withKey));
        var without = await ReadAsync(await client.GetAsync("/v1/nope"));

        Assert.That(known, Is.EqualTo((HttpStatusCode.NotFound, "not_found_error")), "never the web app's page");
        Assert.That(without, Is.EqualTo((HttpStatusCode.Unauthorized, "authentication_error")));
    }

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
