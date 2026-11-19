using System.Net;
using System.Net.Http.Json;
using LouisAgent.Api.Contracts.Site;
using LouisAgent.Api.Endpoints.Routing;
using LouisAgent.Api.Site;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Api.Tests.Site.Places;

/// <summary>
/// F164.2 step 7: a site's search over its own places, <c>GET /v1/site/places?q=</c>, open to every visitor, the
/// places only, from the same index the guide's FindPlace reads, so a search costs no model call.
/// </summary>
public class SitePlaceSearchTests
{
    /// <summary>A test server with this assembly as the site; its pages are the ones these tests declare.</summary>
    private static async Task<WebApplication> StartAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        var app = builder.Build();
        app.MapLouisAgentSite(typeof(SitePlaceSearchTests).Assembly, _ => { }, home: "home");
        await app.StartAsync();
        return app;
    }

    /// <summary>A word finds the places that hold it, each by its address, heading and first words.</summary>
    [Test]
    public async Task AWord_FindsItsPlaces()
    {
        await using var app = await StartAsync();

        var response = await app.GetTestClient().GetAsync("/v1/site/places?q=guide");
        var reply = await response.Content.ReadFromJsonAsync<PlacesReply>();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(reply!.Places, Does.Contain(
                new PlaceFound("/places/the-guide", "The guide", "The guide Louis in the corner")));
        });
    }

    /// <summary>A word of one letter, or none, finds nothing, as the guide's search does.</summary>
    [TestCase("/v1/site/places?q=a")]
    [TestCase("/v1/site/places")]
    public async Task AWordTooShort_FindsNothing(string address)
    {
        await using var app = await StartAsync();

        var reply = await app.GetTestClient().GetFromJsonAsync<PlacesReply>(address);

        Assert.That(reply!.Places, Is.Empty);
    }

    /// <summary>The route is open: a visitor searches the site with no key.</summary>
    [Test]
    public async Task TheRoute_IsOpen()
    {
        await using var app = await StartAsync();

        var endpoint = app.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>()
            .Single(route => route.RoutePattern.RawText == "/v1/site/places");

        Assert.That(RouteAccessExtensions.AccessOf(endpoint), Is.EqualTo(RouteAccess.Open));
    }
}
