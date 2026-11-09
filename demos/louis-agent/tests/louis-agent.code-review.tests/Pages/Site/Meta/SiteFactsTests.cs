using LouisAgent.CodeReview.Pages.Site;
using LouisAgent.Web.Components.Site.Meta;
using LouisAgent.Web.Components.Site.Meta.Schema;

namespace LouisAgent.CodeReview.Tests.Pages.Site.Meta;

/// <summary>
/// The landing tells a search engine who runs the site, what the site is and what software it is about (F123), as
/// structured data the framework serves in the page's head; and an Apple device gets an icon of the right size.
/// </summary>
public class SiteFactsTests
{
    private const string Site = "https://louis-agent.dev/";

    private static string WebRoot => Path.Combine(TestPaths.RepoRoot,
        "demos/louis-agent/web/louis-agent.code-review/wwwroot");

    /// <summary>The landing names who runs the site, the site and the software, each at the site's address.</summary>
    [Test]
    public void TheLanding_SaysWhoRunsItAndWhatItIs()
    {
        using var ctx = SitePageContext.Create();

        var values = ctx.Render<HomePage>().FindComponent<PageHead>().Instance.Schemas ?? [];

        var organization = values.OfType<SchemaOrganization>().Single();
        var site = values.OfType<SchemaWebSite>().Single();
        var software = values.OfType<SchemaSoftwareApplication>().Single();
        Assert.Multiple(() =>
        {
            Assert.That(values, Has.Count.EqualTo(3));
            Assert.That(organization.Url, Is.EqualTo(Site));
            Assert.That(organization.Logo, Is.EqualTo($"{Site}apple-touch-icon.png"));
            Assert.That(organization.SameAs, Does.Contain("https://www.linkedin.com/in/louismarais86"));
            Assert.That(site.Url, Is.EqualTo(Site));
            Assert.That(software.Url, Is.EqualTo(Site));
            Assert.That(software.ApplicationCategory, Is.EqualTo("DeveloperApplication"));
            Assert.That(new[] { organization.Name, site.Name, software.Name }, Has.All.EqualTo("louis-agent"));
        });
    }

    /// <summary>
    /// The head links an Apple touch icon, and the file is the 180 by 180 pixels Apple asks for: its width and height
    /// are the two numbers after the PNG's header.
    /// </summary>
    [Test]
    public void TheTouchIcon_IsLinked_AndAppleSized()
    {
        string index = File.ReadAllText(Path.Combine(WebRoot, "index.html"));
        string head = index[..index.IndexOf("</head>", StringComparison.Ordinal)];

        byte[] png = File.ReadAllBytes(Path.Combine(WebRoot, "apple-touch-icon.png"));
        int width = png[16] << 24 | png[17] << 16 | png[18] << 8 | png[19];
        int height = png[20] << 24 | png[21] << 16 | png[22] << 8 | png[23];

        Assert.Multiple(() =>
        {
            Assert.That(head, Does.Contain("<link rel=\"apple-touch-icon\" href=\"apple-touch-icon.png\" />"));
            Assert.That((width, height), Is.EqualTo((180, 180)));
        });
    }
}
