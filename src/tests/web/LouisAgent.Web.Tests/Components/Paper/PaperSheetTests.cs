using LouisAgent.Testing;
using LouisAgent.Web.Components.Paper;

namespace LouisAgent.Web.Tests.Components.Paper;

/// <summary>
/// A page that prints as itself: on paper an A4 sheet in black on white with the site's frame left off and no
/// button, and on a screen nothing changed. A link on it prints as its QR code in the link's place.
/// </summary>
public class PaperSheetTests
{
    /// <summary>The framework's paper stylesheet, with the colours of paper from its settings.</summary>
    private static string Sheet() =>
        File.ReadAllText(Path.Combine(Styles, "paper/_sheet.scss")) +
        File.ReadAllText(Path.Combine(Styles, "settings/_paper.scss"));

    /// <summary>The framework's global styles.</summary>
    private static readonly string Styles = Path.Combine(TestPaths.RepoRoot, "src/web/LouisAgent.Web/Styles/web");

    /// <summary>The sheet wraps the page and adds nothing else.</summary>
    [Test]
    public void TheSheet_WrapsThePage()
    {
        using var ctx = new BunitContext();

        var sheet = ctx.Render<PaperSheet>(parameters => parameters.AddChildContent("<p>The page.</p>"));

        Assert.That(sheet.Find("div.paper-sheet > p").TextContent, Is.EqualTo("The page."));
    }

    /// <summary>On paper: A4, white under black words, the frame and every button left off.</summary>
    [TestCase("size: A4;")]
    [TestCase(":root:has(.paper-sheet)")]
    [TestCase("--bs-body-bg: #fff;")]
    [TestCase("body:has(.paper-sheet) .site-header")]
    [TestCase(".paper-sheet .btn")]
    [TestCase("counter(page)")]
    public void OnPaper_TheSheetIsA4_InBlackOnWhite(string rule) =>
        Assert.That(Sheet(), Does.Contain(rule));

    /// <summary>A link that prints is the link on a screen and its code on paper, side by side in the page.</summary>
    [Test]
    public void APrintableLink_IsTheLinkAndItsCode()
    {
        using var ctx = new BunitContext();

        var link = ctx.Render<PrintableLink>(parameters => parameters
            .Add(l => l.Href, "https://github.com/bosbevk")
            .Add(l => l.Label, "GitHub")
            .Add(l => l.NewTab, true)
            .AddChildContent("GitHub →"));

        var screen = link.Find("a.screen-link");
        Assert.Multiple(() =>
        {
            Assert.That(screen.TextContent, Is.EqualTo("GitHub →"));
            Assert.That(screen.GetAttribute("target"), Is.EqualTo("_blank"));
            Assert.That(screen.GetAttribute("rel"), Is.EqualTo("noopener"));
            Assert.That(link.FindAll("a.paper-link"), Has.Count.EqualTo(1));
        });
    }
}
