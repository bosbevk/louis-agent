using LouisAgent.Web.Components.Site.Tiles;
using LouisAgent.Web.Components.Staging;

namespace LouisAgent.Web.Tests.Components.Staging;

/// <summary>
/// F134.1: the showroom's start is a stage, and Louis's rule for building it is that what can be shared is the
/// framework's ("like cool components we could reuse later in other projects"). So the stage's light is a part of
/// its own that knows nothing of demos: a light from above, a pool of it on the floor and the floor's lines, in one
/// colour, behind whatever stands on it.
/// </summary>
public class LitStageTests
{
    /// <summary>The stage's styles, as they are written.</summary>
    private static string Styles => File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
        "src/web/louis-agent.web/Components/Staging/LitStage.razor.scss"));

    /// <summary>A stage in this colour, with one thing standing on it.</summary>
    private static IRenderedComponent<LitStage> Stage(BunitContext ctx, TileAccent accent) =>
        ctx.Render<LitStage>(parameters => parameters
            .Add(stage => stage.Accent, accent)
            .AddChildContent("<p class=\"on-stage\">The thing itself</p>"));

    /// <summary>
    /// The stage wears its colour and holds what stands on it. Its light, its pool and its floor are decoration behind
    /// that, which a screen reader passes over.
    /// </summary>
    [Test]
    public void TheStage_WearsItsColour_AndHoldsWhatStandsOnIt()
    {
        using var ctx = new BunitContext();

        var stage = Stage(ctx, TileAccent.Green).Find(".lit-stage");

        Assert.Multiple(() =>
        {
            Assert.That(stage.ClassList, Does.Contain("accent-green"));
            Assert.That(stage.QuerySelector(".on-stage")!.TextContent, Is.EqualTo("The thing itself"));
            Assert.That(stage.QuerySelector(".lit-back")!.GetAttribute("aria-hidden"), Is.EqualTo("true"));
            Assert.That(stage.QuerySelectorAll(".lit-back > *").Select(layer => layer.ClassName),
                Is.EquivalentTo(new[] { "lit-floor", "lit-light", "lit-pool" }));
            Assert.That(stage.QuerySelector(".lit-back .on-stage"), Is.Null, "what stands on it is not behind it");
        });
    }

    /// <summary>With no colour of its own, the stage is lit in the colour of the place it stands in.</summary>
    [Test]
    public void WithNoColour_TheStageTakesTheColourOfItsPlace()
    {
        using var ctx = new BunitContext();

        var stage = Stage(ctx, TileAccent.None).Find(".lit-stage");

        Assert.That(stage.ClassName, Is.EqualTo("lit-stage"));
    }

    /// <summary>The light is drawn in the accent, so it follows whatever colour the stage wears.</summary>
    [Test]
    public void TheLight_IsTheAccents() =>
        Assert.That(Styles, Does.Contain("var(--accent,"));

    /// <summary>
    /// The light changes colour slowly when the stage's colour changes. For a visitor who asked for less motion it
    /// changes at once.
    /// </summary>
    [Test]
    public void ForLessMotion_TheLightChangesAtOnce() =>
        Assert.That(Styles.ReplaceLineEndings("\n"),
            Does.Contain(
                "@media (prefers-reduced-motion: reduce) {\n    .lit-light, .lit-pool { transition: none; }\n}"));
}
