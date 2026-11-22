using LouisAgent.Api.Site;
using LouisAgent.Api.Site.Cards;
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
        "src/site/LouisAgent.Site/wwwroot/index.html"));

    private static readonly RenderedPage Page = new("Measured · louis-agent", "Every number & its \"source\".",
        "<section id=\"top\"><h2>Every number</h2></section>");

    /// <summary>
    /// The head carries the page's own title, description, link card and canonical address, its route.
    /// </summary>
    [Test]
    public void TheHead_IsThePages()
    {
        string html = PageShell.Fill(Index, Page, Site, "/proof", null);

        Assert.Multiple(() =>
        {
            Assert.That(html, Does.Contain("<title>Measured · louis-agent</title>"));
            Assert.That(html, Does.Contain(
                "<meta name=\"description\" content=\"Every number &amp; its &quot;source&quot;.\""));
            Assert.That(html, Does.Contain("<meta property=\"og:title\" content=\"Measured · louis-agent\""));
            Assert.That(html, Does.Contain("<meta property=\"og:url\" content=\"https://louis-agent.dev/proof\""));
            Assert.That(html, Does.Contain("<link rel=\"canonical\" href=\"https://louis-agent.dev/proof\" />"));
            Assert.That(html, Does.Not.Contain("an AI agent framework in C#</title>"), "the landing's title is gone");
        });
    }

    /// <summary>
    /// The page's words sit inside the app's element, hidden after the loading screen (F128.1, Louis: "we used to have
    /// a loader that showed"): a visitor sees the loader until the app starts and replaces both, a crawler reads the
    /// words in the HTML; the rest stays.
    /// </summary>
    [Test]
    public void TheWords_SitInsideTheApp_HiddenBehindTheLoader()
    {
        string html = PageShell.Fill(Index, Page, Site, "/proof", null);

        int app = html.IndexOf("<div id=\"app\">", StringComparison.Ordinal);
        int loader = html.IndexOf("BOOTING LOUIS AGENT", StringComparison.Ordinal);
        int hidden = html.IndexOf("<div class=\"prerendered\" hidden>", StringComparison.Ordinal);
        int words = html.IndexOf("<h2>Every number</h2>", StringComparison.Ordinal);
        Assert.Multiple(() =>
        {
            Assert.That(loader, Is.GreaterThan(app), "the loader shows while the app starts");
            Assert.That(hidden, Is.GreaterThan(loader));
            Assert.That(words, Is.GreaterThan(hidden), "the words are in the HTML, out of sight");
            Assert.That(html.IndexOf("<div id=\"blazor-error-ui\">", StringComparison.Ordinal), Is.GreaterThan(words));
            Assert.That(html, Does.Contain("_framework/blazor.webassembly.js"));
        });
    }

    /// <summary>A page that declares no description keeps the site's own.</summary>
    [Test]
    public void NoDescription_KeepsTheSites()
    {
        string html = PageShell.Fill(Index, Page with { Description = "" }, Site, "/proof", null);

        Assert.That(html, Does.Contain("<meta name=\"description\" content=\"A website for your small business"));
    }

    /// <summary>
    /// A page with a card of its own names that picture in its head, on the site's own address, and says what it
    /// shows; the two size tags stay as the file has them, since every card of a site is one size.
    /// </summary>
    [Test]
    public void ACard_IsThePagesPicture_WithItsDescription()
    {
        var card = new PageCard("social-cards/proof.png", "louis-agent: Measured. Every number & its \"source\".");

        string html = PageShell.Fill(Index, Page, Site, "/proof", card);

        Assert.Multiple(() =>
        {
            Assert.That(html, Does.Contain(
                "<meta property=\"og:image\" content=\"https://louis-agent.dev/social-cards/proof.png\""));
            Assert.That(html, Does.Contain("<meta property=\"og:image:alt\" content=\"louis-agent: Measured. "
                                           + "Every number &amp; its &quot;source&quot;.\""));
            Assert.That(html, Does.Contain("<meta property=\"og:image:width\" content=\"2400\""));
            Assert.That(html, Does.Contain("<meta property=\"og:image:height\" content=\"1260\""));
        });
    }

    /// <summary>A page with no card wears the site's own picture, and the site's own words for it.</summary>
    [Test]
    public void NoCard_KeepsTheSitesPicture()
    {
        string html = PageShell.Fill(Index, Page, Site, "/proof", null);

        Assert.Multiple(() =>
        {
            Assert.That(html, Does.Contain(
                "<meta property=\"og:image\" content=\"https://louis-agent.dev/social-card.png\""));
            Assert.That(html, Does.Contain(
                "<meta property=\"og:image:alt\" content=\"The name louis-agent with a glass ball as its o"));
        });
    }

    /// <summary>
    /// The page's structured data goes into the head (F123), and every page under the landing gets a breadcrumb of
    /// the site, then the page, at the page's own address; the landing gets none.
    /// </summary>
    [Test]
    public void TheHead_CarriesTheStructuredData_AndABreadcrumb()
    {
        var page = Page with { Schemas = ["<script type=\"application/ld+json\">{\"@type\":\"WebSite\"}</script>"] };

        string html = PageShell.Fill(Index, page, Site, "/proof", null);
        string landing = PageShell.Fill(Index, page with { Title = "louis-agent" }, Site, "/", null);

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
