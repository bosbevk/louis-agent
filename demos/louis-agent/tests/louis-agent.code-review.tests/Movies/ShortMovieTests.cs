using LouisAgent.CodeReview.Components.Movies;

namespace LouisAgent.CodeReview.Tests.Movies;

/// <summary>
/// The short film: six scenes, each with its caption and its detail card, the parts numbered in the order the turn
/// reaches them, and the markup pinned to a golden file so a change to the film is a change someone meant.
/// </summary>
public class ShortMovieTests
{
    /// <summary>Six captions and six cards, numbered 1 to 6 in order.</summary>
    [Test]
    public void TheFilm_HasSixScenes_EachWithItsCaptionAndCard()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<ShortMovie>();

        Assert.That(cut.FindAll(".cap b").Select(b => b.TextContent),
            Is.EqualTo(Enumerable.Range(1, 6).Select(n => $"{n} / 6")));
        Assert.That(cut.FindAll(".card b").Select(b => b.TextContent[..1]),
            Is.EqualTo(Enumerable.Range(1, 6).Select(n => $"{n}")));
    }

    /// <summary>The parts the turn reaches are numbered in that order: your program, the catalogue, core, a tool, the ledger.</summary>
    [Test]
    public void TheParts_AreNumbered_InTheOrderTheTurnReachesThem()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<ShortMovie>();

        Assert.That(cut.FindAll(".part i").Select(i => i.TextContent), Is.EqualTo(["1", "2", "3", "4", "5"]));
        Assert.That(cut.FindAll(".part").Where(p => p.QuerySelector("i") is not null).Select(p => p.ClassName),
            Is.EqualTo(["part p-host", "part p-comp", "part p-core core", "part p-tools", "part p-ledger"]));
    }

    /// <summary>The film is one picture to a screen reader, with what happens in it said once.</summary>
    [Test]
    public void TheFilm_IsOnePicture_ToAScreenReader()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<ShortMovie>();

        Assert.That(cut.Find(".movie").GetAttribute("role"), Is.EqualTo("img"));
        Assert.That(cut.Find(".movie").GetAttribute("aria-label"), Does.StartWith("A 36-second loop"));
        Assert.That(cut.Find(".camera").GetAttribute("aria-hidden"), Is.EqualTo("true"));
    }

    [Test]
    public void TheFilm_MatchesTheGoldenFile()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<ShortMovie>();

        Snapshots.Match("Movies/ShortMovie.html", cut.Markup);
    }
}
