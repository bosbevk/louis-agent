using LouisAgent.Api.Site;
using LouisAgent.Api.Site.Rendering;

namespace LouisAgent.Api.Tests.Site;

/// <summary>
/// The site's own index.html takes a rendered page (F121): its title, description, link card and canonical address in
/// the head, and its words inside the app's element, which the app replaces when it starts.
/// </summary>
public class PageShellTests
{
    private static readonly Uri Site = new("https://louis-agent.dev/");

    private static string Index => File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
        "demos/louis-agent/web/louis-agent.code-review/wwwroot/index.html"));

    private static readonly RenderedPage Page = new("Measured · louis-agent", "Every number & its \"source\".",
        "<section id=\"top\"><h2>Every number</h2></section>");

    /// <summary>The head carries the page's own title, description, link card and canonical address.</summary>
    [Test]
    public void TheHead_IsThePages()
    {
        string html = PageShell.Fill(Index, Page, Site, "/proof/rand", "/proof");

        Assert.Multiple(() =>
        {
            Assert.That(html, Does.Contain("<title>Measured · louis-agent</title>"));
            Assert.That(html, Does.Contain(
                "<meta name=\"description\" content=\"Every number &amp; its &quot;source&quot;.\""));
            Assert.That(html, Does.Contain("<meta property=\"og:title\" content=\"Measured · louis-agent\""));
            Assert.That(html, Does.Contain("<meta property=\"og:url\" content=\"https://louis-agent.dev/proof/rand\""));
            Assert.That(html, Does.Contain("<link rel=\"canonical\" href=\"https://louis-agent.dev/proof/rand\" />"));
            Assert.That(html, Does.Not.Contain("an AI agent framework in C#</title>"), "the landing's title is gone");
        });
    }

    /// <summary>
    /// The page's words sit inside the app's element, in place of the loading screen; the rest stays.
    /// </summary>
    [Test]
    public void TheWords_SitInsideTheApp()
    {
        string html = PageShell.Fill(Index, Page, Site, "/proof", "/proof");

        int app = html.IndexOf("<div id=\"app\">", StringComparison.Ordinal);
        Assert.Multiple(() =>
        {
            Assert.That(html.IndexOf("<h2>Every number</h2>", StringComparison.Ordinal), Is.GreaterThan(app));
            Assert.That(html, Does.Not.Contain("BOOTING LOUIS AGENT"));
            Assert.That(html, Does.Contain("<div id=\"blazor-error-ui\">"));
            Assert.That(html, Does.Contain("_framework/blazor.webassembly.js"));
        });
    }

    /// <summary>A page that declares no description keeps the site's own.</summary>
    [Test]
    public void NoDescription_KeepsTheSites()
    {
        string html = PageShell.Fill(Index, Page with { Description = "" }, Site, "/proof", "/proof");

        Assert.That(html, Does.Contain("<meta name=\"description\" content=\"An AI agent framework in C#"));
    }

    /// <summary>
    /// The page's structured data goes into the head (F123), and every page under the landing gets a breadcrumb of
    /// the site, then the page, at the page's own address; the landing gets none.
    /// </summary>
    [Test]
    public void TheHead_CarriesTheStructuredData_AndABreadcrumb()
    {
        var page = Page with { Schemas = ["<script type=\"application/ld+json\">{\"@type\":\"WebSite\"}</script>"] };

        string html = PageShell.Fill(Index, page, Site, "/proof/rand", "/proof");
        string landing = PageShell.Fill(Index, page with { Title = "louis-agent" }, Site, "/home/custom", "/");

        string head = html[..html.IndexOf("</head>", StringComparison.Ordinal)];
        Assert.Multiple(() =>
        {
            Assert.That(head, Does.Contain("{\"@type\":\"WebSite\"}</script>"));
            Assert.That(head, Does.Contain("\"@type\":\"BreadcrumbList\""));
            Assert.That(head, Does.Contain("\"name\":\"louis-agent\",\"item\":\"https://louis-agent.dev/\""));
            Assert.That(head, Does.Contain("\"name\":\"Measured\",\"item\":\"https://louis-agent.dev/proof\""));
            Assert.That(landing, Does.Not.Contain("BreadcrumbList"));
        });
    }
}
