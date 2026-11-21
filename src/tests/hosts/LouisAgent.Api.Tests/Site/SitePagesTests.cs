using System.Net;
using LouisAgent.Api.Config;
using LouisAgent.Api.Site;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;

namespace LouisAgent.Api.Tests.Site;

/// <summary>
/// A site's pages are served as HTML (F121): a page's address answers its own rendered page in the site's
/// index.html, a section's address its page, a page that declares no description the plain shell, an address with no
/// page a 404, and a page that fails the plain shell, so a crawler always gets a page and a visitor always gets the
/// site.
/// </summary>
public class SitePagesTests
{
    private const string Index =
        "<!DOCTYPE html><html><head><title>Site</title><meta name=\"description\" content=\"The site.\" />" +
        "<meta property=\"og:url\" content=\"https://site.test/\" /></head>" +
        "<body><div id=\"app\"><div>Loading</div></div>" +
        "<script src=\"_framework/blazor.webassembly.js\"></script></body></html>";

    /// <summary>A page meant for search: its title, description and words.</summary>
    [Route("/sample")]
    public sealed class SamplePage : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<PageTitle>(0);
            builder.AddComponentParameter(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Sample · site")));
            builder.CloseComponent();
            builder.OpenComponent<HeadContent>(2);
            builder.AddComponentParameter(3, "ChildContent", (RenderFragment)(b =>
                b.AddMarkupContent(0, "<meta name=\"description\" content=\"A sample page of the site.\" />")));
            builder.CloseComponent();
            builder.AddMarkupContent(4, "<section id=\"top\"><h2>Sample words</h2></section>");
        }
    }

    /// <summary>A page that declares no description, such as a chat.</summary>
    [Route("/plain")]
    public sealed class PlainPage : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, "Plain words");
    }

    /// <summary>A page that fails while it renders.</summary>
    [Route("/broken")]
    public sealed class BrokenPage : ComponentBase
    {
        protected override void OnInitialized() => throw new InvalidOperationException("broken");
    }

    /// <summary>A site that names no public address gets canonical links on the address it was asked at.</summary>
    [Test]
    public async Task NoPublicAddress_UsesTheRequests()
    {
        var (app, client, root) = await StartAsync(publicUrl: null);
        await using var _ = app;

        string html = await client.GetStringAsync("/sample");

        Assert.That(html, Does.Contain("<link rel=\"canonical\" href=\"http://localhost/sample\" />"));
        Directory.Delete(root, recursive: true);
    }

    /// <summary>
    /// A site that names its address in SITE_ADDRESS (FX216) gets canonical links on it, and a page asked for on any
    /// other host, such as www, is sent there for good with its path and question; a file and the API stay where they
    /// are, since a health probe and a chat on another site call them there.
    /// </summary>
    [Test]
    public async Task ASiteAddress_IsTheOnlyHostAPageAnswersOn()
    {
        var (app, client, root) = await StartAsync(publicUrl: null, siteAddress: "https://site.test");
        await using var _ = app;

        string html = await client.GetStringAsync("https://site.test/sample");
        var page = await client.GetAsync("https://www.site.test/sample/top?from=mail");
        var file = await client.GetAsync("https://www.site.test/index.html");
        var api = await client.GetAsync("https://www.site.test/v1/health");

        Assert.Multiple(() =>
        {
            Assert.That(html, Does.Contain("<link rel=\"canonical\" href=\"https://site.test/sample\" />"));
            Assert.That(page.StatusCode, Is.EqualTo(HttpStatusCode.MovedPermanently));
            Assert.That(page.Headers.Location, Is.EqualTo(new Uri("https://site.test/sample/top?from=mail")));
            Assert.That(file.StatusCode, Is.EqualTo(HttpStatusCode.OK), "a file is never sent on");
            Assert.That(api.StatusCode, Is.EqualTo(HttpStatusCode.OK), "the API is never sent on");
        });
        Directory.Delete(root, recursive: true);
    }

    /// <summary>A stand-in for an API route on the same host as the site.</summary>
    private static string Health() => "ok";

    /// <summary>
    /// A test server with this assembly as the site, its index.html in a web root of its own, an API route beside it,
    /// and SITE_ADDRESS in its settings when one is given.
    /// </summary>
    private static async Task<(WebApplication App, HttpClient Client, string Root)> StartAsync(
        string? publicUrl = "https://site.test/", string? siteAddress = null)
    {
        string root = Path.Combine(Path.GetTempPath(), "site-pages-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(Path.Combine(root, "index.html"), Index);
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { WebRootPath = root });
        builder.WebHost.UseTestServer();
        if (siteAddress is not null) builder.Configuration[SiteAddressOptions.Setting] = siteAddress;
        var app = builder.Build();
        // A file and an API route beside the pages, as a real host has.
        app.UseStaticFiles();
        app.MapGet("/v1/health", Health);
        app.MapLouisAgentSite(typeof(SitePagesTests).Assembly, _ => { },
            publicUrl is null ? null : new Uri(publicUrl), "home");
        await app.StartAsync();
        // The client asks at the site's own address, as a visitor who followed its links does.
        var client = app.GetTestClient();
        string? site = publicUrl ?? siteAddress;
        if (site is not null) client.BaseAddress = new Uri(site);
        return (app, client, root);
    }

    /// <summary>A page's address answers its own title, description, canonical address and words.</summary>
    [Test]
    public async Task APage_AnswersItsOwnHtml()
    {
        var (app, client, root) = await StartAsync();
        await using var _ = app;

        var response = await client.GetAsync("/sample");
        string html = await response.Content.ReadAsStringAsync();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/html"));
            Assert.That(html, Does.Contain("<title>Sample · site</title>"));
            Assert.That(html, Does.Contain("content=\"A sample page of the site.\""));
            Assert.That(html, Does.Contain("<link rel=\"canonical\" href=\"https://site.test/sample\" />"));
            Assert.That(html, Does.Contain("Loading").And.Contain("<div class=\"prerendered\" hidden>"),
                "the loader shows, the words wait out of sight (F128.1)");
            Assert.That(html, Does.Contain("<h2>Sample words</h2>"));
        });
        Directory.Delete(root, recursive: true);
    }

    /// <summary>
    /// A section's address answers its page, and names the page as the original, however it was spelled (FX216:
    /// Google folds copies that each claim to be the original into one of its own choosing).
    /// </summary>
    [Test]
    public async Task ASectionsAddress_AnswersItsPage_WithThePageAsCanonical()
    {
        var (app, client, root) = await StartAsync();
        await using var _ = app;

        string section = await client.GetStringAsync("/sample/top");
        string slashed = await client.GetStringAsync("/sample/top/");

        Assert.Multiple(() =>
        {
            Assert.That(section, Does.Contain("<h2>Sample words</h2>")
                .And.Contain("<link rel=\"canonical\" href=\"https://site.test/sample\" />"));
            Assert.That(slashed, Does.Contain("<link rel=\"canonical\" href=\"https://site.test/sample\" />"));
        });
        Directory.Delete(root, recursive: true);
    }

    /// <summary>
    /// A page with no description gets the plain shell, as does one that fails; an address with no page is a 404.
    /// </summary>
    [Test]
    public async Task PlainBrokenAndMissing_AreAnsweredHonestly()
    {
        var (app, client, root) = await StartAsync();
        await using var _ = app;

        var plain = await client.GetAsync("/plain");
        var broken = await client.GetAsync("/broken");
        var missing = await client.GetAsync("/nowhere/at/all");

        Assert.Multiple(async () =>
        {
            Assert.That(plain.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(await plain.Content.ReadAsStringAsync(),
                Does.Contain("Loading").And.Not.Contain("Plain words"));
            Assert.That(broken.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(await broken.Content.ReadAsStringAsync(), Does.Contain("Loading"));
            Assert.That(missing.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(await missing.Content.ReadAsStringAsync(), Does.Contain("blazor.webassembly.js"));
        });
        Directory.Delete(root, recursive: true);
    }
}
