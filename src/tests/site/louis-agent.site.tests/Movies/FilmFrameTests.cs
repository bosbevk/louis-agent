using LouisAgent.Site.Components.Movies;
using LouisAgent.Site.Pages;

namespace LouisAgent.Site.Tests.Movies;

/// <summary>
/// A film plays in a frame with one control, pause and play (Louis, 2026-11-08: "can we pause these films?"). The
/// frame sits around the film, never inside it, so the film's markup, its golden and its export stay as they are.
/// </summary>
public class FilmFrameTests
{
    /// <summary>The film is inside the frame, playing, and the button offers to pause it.</summary>
    [Test]
    public void TheFilm_PlaysInTheFrame_AndTheButtonOffersToPause()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<FilmFrame>(parameters => parameters.AddChildContent("<div class=\"movie\">Film</div>"));

        var button = cut.Find("button.film-pause");
        Assert.That(cut.FindAll(".film-frame > .movie"), Has.Count.EqualTo(1));
        Assert.That(cut.Find(".film-frame").ClassList, Does.Not.Contain("is-paused"));
        Assert.That(button.GetAttribute("aria-pressed"), Is.EqualTo("false"));
        Assert.That(button.GetAttribute("aria-label"), Is.EqualTo("Pause the film"));
    }

    /// <summary>One press pauses, the next plays again.</summary>
    [Test]
    public void APress_Pauses_AndTheNextPlays()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<FilmFrame>(parameters => parameters.AddChildContent("<div class=\"movie\">Film</div>"));

        cut.Find("button.film-pause").Click();
        Assert.That(cut.Find(".film-frame").ClassList, Does.Contain("is-paused"));
        Assert.That(cut.Find("button.film-pause").GetAttribute("aria-pressed"), Is.EqualTo("true"));
        Assert.That(cut.Find("button.film-pause").GetAttribute("aria-label"), Is.EqualTo("Play the film"));

        cut.Find("button.film-pause").Click();
        Assert.That(cut.Find(".film-frame").ClassList, Does.Not.Contain("is-paused"));
    }

    /// <summary>Both pages play their film in the frame, so both can be paused; the film itself is unchanged.</summary>
    [TestCase(typeof(HomePage), "film-short")]
    [TestCase(typeof(HowItWorksPage), "film-detail")]
    public void APage_PlaysItsFilm_InTheFrame(Type page, string film)
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render(builder =>
        {
            builder.OpenComponent(0, page);
            builder.CloseComponent();
        });

        Assert.That(cut.FindAll($".film-frame > .movie > .stage.{film}"), Has.Count.EqualTo(1));
        Assert.That(cut.FindAll(".movie button"), Is.Empty, "the control is outside the film");
    }

    /// <summary>
    /// The film fits a slide's page under its section's heading, whatever the screen's height (FX112, FX118): the
    /// frame is never wider than the room under a heading is high at 16:9, so the film, which scales to its frame's
    /// width, sits on the page with its title and never leaves the title alone on a page of its own.
    /// </summary>
    [Test]
    public void TheFrame_FitsUnderItsHeading()
    {
        string styles = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "src/site/louis-agent.site/Components/Movies/FilmFrame.razor.scss"));

        Assert.That(styles, Does.Contain("max-width: calc((100dvh - 500px - var(--site-bar, 0px)) * 16 / 9);"));
    }
}
