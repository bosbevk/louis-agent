using System.Text.RegularExpressions;
using LouisAgent.FilmExport.Cards.Pages;

namespace LouisAgent.FilmExport.Tests.Cards.Pages;

/// <summary>
/// The two cards' pages (F169.6): a page's card and the site's own, each a template filled with the site's name,
/// photographed from the running site, and the page's words. Both are drawn in flat's light look, every word starting
/// at the same left edge, in Geist.
/// </summary>
public class PageCardPageTests
{
    private static readonly CardName Name = new([1, 2, 3], 210.5, 52);

    private static readonly PageCardWords Words = new("louis-agent", "Getting started", "Your agent, in two lines.",
        "framework.louis-agent.dev/getting-started");

    private static string Template(string path) => File.ReadAllText(Path.Combine(TestPaths.RepoRoot, path));

    /// <summary>The placeholders a template holds, such as {name} or {page}.</summary>
    private static readonly Regex Hole = new(@"\{[a-z-]+\}");

    /// <summary>
    /// Filled, a page's card holds the name's photograph once, at its size, the page's name, its heading and its
    /// address, and no placeholder is left.
    /// </summary>
    [Test]
    public void APagesCard_HoldsTheNameOnce_AndThePagesWords()
    {
        string page = PageCardPage.ForPage(Template(PageCardPage.PageTemplatePath), Name, Words);

        Assert.Multiple(() =>
        {
            Assert.That(page.Split("data:image/png;base64,AQID").Length - 1, Is.EqualTo(1), "the name, once");
            Assert.That(page, Does.Contain("width: 210.5px").And.Contain("height: 52px"));
            Assert.That(page, Does.Contain(">Getting started<"));
            Assert.That(page, Does.Contain(">Your agent, in two lines.<"));
            Assert.That(page, Does.Contain(">framework.louis-agent.dev/getting-started<"));
            Assert.That(Hole.IsMatch(page), Is.False, "no placeholder is left");
        });
    }

    /// <summary>The site's card holds the name once, its own line and the site's address.</summary>
    [Test]
    public void TheSitesCard_SaysTheFrameworksLine()
    {
        string page = PageCardPage.ForSite(Template(PageCardPage.SiteTemplatePath), Name, "framework.louis-agent.dev");

        Assert.Multiple(() =>
        {
            Assert.That(page.Split("data:image/png;base64,AQID").Length - 1, Is.EqualTo(1), "the name, once");
            Assert.That(page, Does.Contain(">The C# AI agent framework<"));
            Assert.That(page, Does.Contain(">framework.louis-agent.dev<"));
            Assert.That(Hole.IsMatch(page), Is.False, "no placeholder is left");
        });
    }

    /// <summary>Words with an ampersand or an angle bracket are made safe, so they show as written.</summary>
    [Test]
    public void ThePagesWords_AreMadeSafe()
    {
        var words = Words with { Page = "Cost & evals", Heading = "Every <turn>, measured." };

        string page = PageCardPage.ForPage(Template(PageCardPage.PageTemplatePath), Name, words);

        Assert.That(page, Does.Contain("Cost &amp; evals").And.Contain("Every &lt;turn&gt;, measured."));
    }

    /// <summary>
    /// Each colour in both pages is one flat's light sheet sets, so a changed theme value fails here until the card
    /// follows it.
    /// </summary>
    [TestCase(PageCardPage.PageTemplatePath)]
    [TestCase(PageCardPage.SiteTemplatePath)]
    public void EveryColour_IsFlatsLight(string path)
    {
        string flat = Template("src/web/LouisAgent.Web.Themes/Styles/theme-flat/_light.scss").ToLowerInvariant();

        var colours = Regex.Matches(Template(path), "#[0-9a-fA-F]{6}\\b")
            .Select(match => match.Value.ToLowerInvariant()).Distinct().ToList();

        Assert.Multiple(() =>
        {
            Assert.That(colours, Is.Not.Empty);
            Assert.That(colours.Where(colour => !flat.Contains(colour)), Is.Empty, "a colour flat's light doesn't set");
        });
    }

    /// <summary>
    /// Every word starts 128 points from the left, the type is Geist and Geist Mono, and the band along the foot runs
    /// edge to edge.
    /// </summary>
    [TestCase(PageCardPage.PageTemplatePath)]
    [TestCase(PageCardPage.SiteTemplatePath)]
    public void EveryWord_StartsAt128_InGeist(string path)
    {
        string page = Template(path);

        var lefts = Regex.Matches(page, @"left:\s*([0-9.]+)px").Select(match => match.Groups[1].Value).Distinct();
        var faces = Regex.Matches(page, "font-family:\\s*\"([^\"]+)\"").Select(match => match.Groups[1].Value)
            .Distinct();

        Assert.Multiple(() =>
        {
            Assert.That(lefts, Is.EqualTo(new[] { "128" }));
            Assert.That(faces, Is.SubsetOf(new[] { "Geist", "Geist Mono" }));
            Assert.That(page, Does.Contain("family=Geist:").And.Contain("family=Geist+Mono:"));
        });
    }
}
