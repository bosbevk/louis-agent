namespace LouisAgent.Site.Tests.Pages.Meta;

/// <summary>
/// The site's small marks (F151.6): the tab icon, the app icons and the share card are the flat violet disc with the
/// white l, the mark Louis picked for every small size, in place of the three slabs. The brand's folder holds the
/// same drawing.
/// </summary>
public class SiteIconsTests
{
    private static string WebRoot => Path.Combine(TestPaths.RepoRoot, "src/site/louis-agent.site/wwwroot");

    /// <summary>The l, as Geist draws it at weight 650, the same letter the glass ball carries.</summary>
    private const string TheL = "M68 133 68.5 710H207.5V147.5";

    /// <summary>The disc in the brand's violet, Ultraviolet's middle step.</summary>
    private const string TheDisc = "fill=\"#6b4dff\"";

    /// <summary>The tab icon, the site's own mark and the brand's are the violet disc with the white l.</summary>
    [TestCase("src/site/louis-agent.site/wwwroot/favicon.svg")]
    [TestCase("src/site/louis-agent.site/wwwroot/brand/mark.svg")]
    [TestCase("src/docs/brand/mark.svg")]
    public void TheMark_IsTheVioletDiscWithTheWhiteL(string file)
    {
        string svg = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, file));

        Assert.Multiple(() =>
        {
            Assert.That(svg, Does.Contain(TheDisc));
            Assert.That(svg, Does.Contain(TheL));
            Assert.That(svg, Does.Not.Contain("M32 9 L54 20"), "the three slabs are gone");
        });
    }

    /// <summary>The app icon a browser lists is 192 pixels square, the size the page's head names.</summary>
    [Test]
    public void TheAppIcon_Is192Square()
    {
        byte[] png = File.ReadAllBytes(Path.Combine(WebRoot, "icon-192.png"));
        int width = png[16] << 24 | png[17] << 16 | png[18] << 8 | png[19];
        int height = png[20] << 24 | png[21] << 16 | png[22] << 8 | png[23];

        Assert.That((width, height), Is.EqualTo((192, 192)));
    }
}
