using LouisAgent.Web.Components.Site.Brand;

namespace LouisAgent.Web.Tests.Components.Site.Brand;

/// <summary>
/// A site's name with the mark standing in for one of its letters (F151.6), as louis-agent's o: what stands before
/// the mark, the mark with no letter on it, and what stands after. A screen reader is told the whole name once.
/// </summary>
public class WordmarkTests
{
    /// <summary>The name as it is drawn for louis-agent.</summary>
    private static IRenderedComponent<Wordmark> Render(BunitContext ctx) =>
        ctx.Render<Wordmark>(parameters => parameters
            .Add(wordmark => wordmark.Before, "l")
            .Add(wordmark => wordmark.After, "uis-agent"));

    /// <summary>What is seen is the letters before, the bare mark, and the letters after, in that order.</summary>
    [Test]
    public void TheNameIsDrawn_WithTheMarkBetweenItsLetters()
    {
        using var ctx = new BunitContext();

        var seen = Render(ctx).Find(".wordmark .wordmark-seen");

        Assert.Multiple(() =>
        {
            Assert.That(seen.TextContent, Is.EqualTo("luis-agent"), "the letters, and nothing on the mark");
            Assert.That(seen.ChildNodes.Select(node => node.NodeName),
                Is.EqualTo(new[] { "#text", "SPAN", "#text" }), "before, the mark, after");
            Assert.That(seen.QuerySelector(".orb")!.TextContent, Is.Empty);
        });
    }

    /// <summary>A screen reader hears the whole name once: the drawn letters and the mark are hidden from it.</summary>
    [Test]
    public void AScreenReader_IsToldTheWholeNameOnce()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx);

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find(".wordmark .wordmark-name").TextContent, Is.EqualTo("louis-agent"));
            Assert.That(cut.Find(".wordmark .wordmark-seen").GetAttribute("aria-hidden"), Is.EqualTo("true"));
        });
    }

    /// <summary>The letter the mark stands for is an o unless the site says another.</summary>
    [Test]
    public void TheMark_StandsForTheLetterTheSiteNames()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<Wordmark>(parameters => parameters
            .Add(wordmark => wordmark.Before, "gr")
            .Add(wordmark => wordmark.Letter, "a")
            .Add(wordmark => wordmark.After, "pe"));

        Assert.That(cut.Find(".wordmark-name").TextContent, Is.EqualTo("grape"));
    }

    /// <summary>
    /// FX184: the mark's size as a letter is the theme's to say (Louis: "circle too big", his pick of 0.64em for
    /// flat), and it sits lower the bigger it is, so whatever its size it stands on the line with the letters.
    /// </summary>
    [Test]
    public void TheMarksSize_IsTheThemes_AndItsDropFollowsIt()
    {
        string styles = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "src/web/LouisAgent.Web/Components/Site/Brand/Wordmark.razor.scss"));

        Assert.Multiple(() =>
        {
            Assert.That(styles, Does.Contain("width: var(--web-wordmark-o);"));
            Assert.That(styles, Does.Contain("height: var(--web-wordmark-o);"));
            Assert.That(styles, Does.Contain("translate: 0 calc((var(--web-wordmark-o) - .54em) * .385);"));
        });
    }
}
