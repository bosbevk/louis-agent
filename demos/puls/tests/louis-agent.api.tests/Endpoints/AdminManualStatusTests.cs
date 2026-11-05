using System.Net;
using LouisAgent.Api.Endpoints;
using LouisAgent.Api.Evals.List;
using LouisAgent.Choices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;

namespace LouisAgent.Api.Tests.Endpoints;

/// <summary>
/// Pins when the manual route says 404: for a check that isn't in the catalogue, and for one that is but whose manual
/// file isn't there.
/// </summary>
public class AdminManualStatusTests
{
    /// <summary>The admin key the test host is built with, and that each request sends.</summary>
    private const string AdminKey = "admin-secret";

    /// <summary>A check we know about but whose manual file is missing has nothing to show, so it's a 404.</summary>
    [Test]
    public async Task AKnownCheck_WhoseManualFileIsMissing_Is404()
    {
        var catalog = new EvalList([new EvalEntry("ghost", "Ghost", "evals/ghost/Manuals/ghost.md")]);
        await using var app = ApiTestHost.Build(adminKey: AdminKey, catalog: catalog);
        await app.StartAsync();

        using var response = await GetAsync(app, "/v1/admin/benchmarks/ghost/manual");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(await response.Content.ReadAsStringAsync(), Does.Contain(ApiErrorKind.NotFound.Wire()));
    }

    /// <summary>A check we don't know is a 404 too, and never reads a file named by the caller.</summary>
    [Test]
    public async Task AnUnknownCheck_Is404()
    {
        await using var app = ApiTestHost.Build(adminKey: AdminKey);
        await app.StartAsync();

        using var response = await GetAsync(app, "/v1/admin/benchmarks/nothing/manual");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>Asks the admin route with the key.</summary>
    private static Task<HttpResponseMessage> GetAsync(WebApplication app, string path)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("x-api-key", AdminKey);

        return app.GetTestClient().SendAsync(request);
    }
}
