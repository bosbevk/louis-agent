namespace LouisAgent.SiteFramework.Tests.Meta;

/// <summary>
/// The framework's site names an icon Google Search can show (FX217). Google takes one icon per host, from the home
/// page's head, and reads only bitmaps (PNG, ICO and the like), never an SVG, so an SVG alone leaves a grey globe beside
/// every result. The site names the same PNG marks louis-agent.dev does, the violet disc with the white l, beside its
/// SVG for the browsers that take one.
/// </summary>
public class SiteIconsTests
{
    /// <summary>A file of the site's own, served as it is.</summary>
    private static string Served(string file) =>
        Path.Combine(TestPaths.RepoRoot, "src/site/LouisAgent.SiteFramework/wwwroot", file);

    /// <summary>
    /// The head names a PNG icon and an Apple touch icon, each a file the site serves, square and at least 48 pixels,
    /// the size Google asks for.
    /// </summary>
    [TestCase("<link rel=\"icon\" type=\"image/png\" href=\"icon-192.png\" />", "icon-192.png", 192)]
    [TestCase("<link rel=\"apple-touch-icon\" href=\"apple-touch-icon.png\" />", "apple-touch-icon.png", 180)]
    public void TheHead_NamesABitmapIcon_GoogleCanShow(string link, string file, int side)
    {
        string head = File.ReadAllText(Served("index.html"));
        byte[] png = File.ReadAllBytes(Served(file));
        int width = png[16] << 24 | png[17] << 16 | png[18] << 8 | png[19];
        int height = png[20] << 24 | png[21] << 16 | png[22] << 8 | png[23];

        Assert.Multiple(() =>
        {
            Assert.That(head, Does.Contain(link));
            Assert.That((width, height), Is.EqualTo((side, side)));
        });
    }

    /// <summary>The icons are louis-agent.dev's own, so both sites show one mark.</summary>
    [TestCase("icon-192.png")]
    [TestCase("apple-touch-icon.png")]
    [TestCase("favicon.svg")]
    public void TheIcon_IsLouisAgentDevs(string file)
    {
        string site = Path.Combine(TestPaths.RepoRoot, "src/site/LouisAgent.Site/wwwroot", file);

        Assert.That(File.ReadAllBytes(Served(file)), Is.EqualTo(File.ReadAllBytes(site)));
    }
}
