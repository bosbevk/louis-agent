using LouisAgent.Web.Components.Site.Turns;

namespace LouisAgent.Web.Tests.Components.Site.Turns;

/// <summary>
/// The window a turn plays in is a glass panel like the others (Louis: "this panel should also have the tilt option",
/// "all glass panels must have the reflection option"): it bends and leans until a site switches that off, and shows
/// its reflection when a site switches that on.
/// </summary>
public class LiveTurnTests
{
    /// <summary>One line is enough to draw the window.</summary>
    private static readonly IReadOnlyList<TurnLine> OneLine = [new TurnLine(TurnLineKind.Prompt, "fix the test", 0)];

    /// <summary>With nothing switched, the window bends and leans, and shows no reflection.</summary>
    [Test]
    public void TheWindow_BendsAndLeans_ByDefault()
    {
        using var ctx = new BunitContext();

        var window = ctx.Render<LiveTurn>(p => p.Add(t => t.Title, "session").Add(t => t.Label, "a turn")
            .Add(t => t.Lines, OneLine)).Find(".live-turn");

        Assert.Multiple(() =>
        {
            Assert.That(window.HasAttribute("data-tilt"), Is.True);
            Assert.That(window.HasAttribute("data-lens"), Is.False);
            Assert.That(window.HasAttribute("data-reflect"), Is.False);
        });
    }

    /// <summary>Switched, the window stands still, bends nothing, and shows its reflection.</summary>
    [Test]
    public void TheWindow_Switched_IsStill_AndReflected()
    {
        using var ctx = new BunitContext();

        var window = ctx.Render<LiveTurn>(p => p.Add(t => t.Title, "session").Add(t => t.Label, "a turn")
            .Add(t => t.Lines, OneLine).Add(t => t.Tilt, false).Add(t => t.Lens, false).Add(t => t.Reflect, true))
            .Find(".live-turn");

        Assert.Multiple(() =>
        {
            Assert.That(window.HasAttribute("data-tilt"), Is.False);
            Assert.That(window.GetAttribute("data-lens"), Is.EqualTo("off"));
            Assert.That(window.HasAttribute("data-reflect"), Is.True);
        });
    }
}
