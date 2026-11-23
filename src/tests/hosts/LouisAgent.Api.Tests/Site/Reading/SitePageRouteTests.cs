using System.Net;
using System.Net.Http.Json;
using LouisAgent.Api.Site;
using LouisAgent.Composition.Sites;
using LouisAgent.SessionStream.Wire.Requests;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Api.Tests.Site.Reading;

/// <summary>
/// F126.3.3: the site's API answers a page as a message carries it, <c>GET /v1/site/page?address=</c>, open as the
/// site's search is, so an eval's run sends its agent the page production's agent gets.
/// </summary>
public class SitePageRouteTests
{
    /// <summary>How many times the reading page was rendered, so a test can see it rendered once.</summary>
    private static int _renders;

    /// <summary>A page with a title, a section and a link in its main part.</summary>
    [Route("/reading")]
    public sealed class ReadingPage : ComponentBase
    {
        /// <summary>Counts the render.</summary>
        protected override void OnInitialized() => Interlocked.Increment(ref _renders);

        /// <summary>Its title, then its main part.</summary>
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<PageTitle>(0);
            builder.AddComponentParameter(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Reading · site")));
            builder.CloseComponent();
            builder.AddMarkupContent(2, "<main><section id=\"plans\"><h2>Our plans</h2><p>Two plans.</p>" +
                                        "<a href=\"/contact\">Talk to us</a></section></main>");
        }
    }

    /// <summary>An address of a page answers that page: its address, title, words, areas and links.</summary>
    [Test]
    public async Task AnAddress_AnswersItsPage()
    {
        await using var app = await StartAsync(_ => { });

        var response = await app.GetTestClient().GetAsync("/v1/site/page?address=/reading");
        var page = await response.Content.ReadFromJsonAsync<PageRequest>();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That((page!.Url, page.Title, page.Text),
                Is.EqualTo(("/reading", "Reading · site", "Our plans Two plans. Talk to us")));
            Assert.That(page.Areas, Is.EqualTo(new[] { new PageAreaRequest("plans", "Our plans") }));
            Assert.That(page.Links, Is.EqualTo(new[] { new PageAreaRequest("/contact", "Talk to us") }));
            Assert.That(page.Focus, Is.Null, "the site names no focuses");
        });
    }

    /// <summary>The focus is the site's rule's answer for the address.</summary>
    [Test]
    public async Task TheFocus_IsTheSitesRule_ForTheAddress()
    {
        var focuses = new SiteFocuses("selling", [new FocusPart("/reading", "reading")]);
        await using var app = await StartAsync(services => services.AddSingleton(focuses));

        var page = await app.GetTestClient().GetFromJsonAsync<PageRequest>("/v1/site/page?address=/reading");

        Assert.That(page!.Focus, Is.EqualTo("reading"));
    }

    /// <summary>A place inside a page answers the page that holds it.</summary>
    [Test]
    public async Task APlaceInsideAPage_AnswersThePageThatHoldsIt()
    {
        await using var app = await StartAsync(_ => { });

        var page = await app.GetTestClient().GetFromJsonAsync<PageRequest>("/v1/site/page?address=/reading/plans");

        Assert.That(page!.Url, Is.EqualTo("/reading"));
    }

    /// <summary>An address the site has no page for, or no address at all, is not found.</summary>
    [TestCase("/v1/site/page?address=/nowhere")]
    [TestCase("/v1/site/page")]
    public async Task NoSuchPage_IsNotFound(string address)
    {
        await using var app = await StartAsync(_ => { });

        var response = await app.GetTestClient().GetAsync(address);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>Asked twice for one address, the page is rendered once and kept.</summary>
    [Test]
    public async Task AskedTwice_ThePageIsRenderedOnce()
    {
        await using var app = await StartAsync(_ => { });
        var client = app.GetTestClient();
        int before = _renders;

        await client.GetAsync("/v1/site/page?address=/reading");
        await client.GetAsync("/v1/site/page?address=/reading");

        Assert.That(_renders - before, Is.EqualTo(1));
    }

    /// <summary>A test server with this assembly as the site, its services as given; the route needs no key.</summary>
    private static async Task<WebApplication> StartAsync(Action<IServiceCollection> services)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        var app = builder.Build();
        app.MapLouisAgentSite(typeof(SitePageRouteTests).Assembly, services, home: "home");
        await app.StartAsync();
        return app;
    }
}
