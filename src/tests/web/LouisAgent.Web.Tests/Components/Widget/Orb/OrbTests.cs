using LouisAgent.Web.Components.Widget.Orb;

namespace LouisAgent.Web.Tests.Components.Widget.Orb;

/// <summary>
/// F134.3: the agent's orb shows what the agent is doing. Idle it breathes, thinking an arc goes round, showing rings
/// go out to the page, speaking it pulses, and since F151.4 it listens too, with a ring in the visitor's colour. The
/// state is read from the chat's own state by a rule, never guessed, and the disc is drawn from the names a theme
/// sets for the mark.
/// </summary>
public class OrbTests
{
    /// <summary>The orb's own sheet, its line ends folded.</summary>
    private static string Styles => File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
        "src/web/LouisAgent.Web/Components/Widget/Orb/Orb.razor.scss")).ReplaceLineEndings("\n");

    /// <summary>The orb wears its state as a class the styles read, and a screen reader hears it.</summary>
    [TestCase(OrbState.Idle, "orb is-idle")]
    [TestCase(OrbState.Listening, "orb is-listening")]
    [TestCase(OrbState.Thinking, "orb is-thinking")]
    [TestCase(OrbState.Showing, "orb is-showing")]
    [TestCase(OrbState.Speaking, "orb is-speaking")]
    public void TheOrb_WearsItsState(OrbState state, string classes)
    {
        using var ctx = new BunitContext();

        var orb = ctx.Render<LouisAgent.Web.Components.Widget.Orb.Orb>(parameters => parameters
            .Add(o => o.State, state)
            .Add(o => o.Label, "the guide")).Find(".orb");

        Assert.Multiple(() =>
        {
            Assert.That(orb.ClassName, Is.EqualTo(classes));
            Assert.That(orb.GetAttribute("aria-label"), Does.StartWith("the guide"));
        });
    }

    /// <summary>
    /// The orb carries the agent's mark on its disc, as the design drew the guide's initial, and nothing when it is
    /// given none.
    /// </summary>
    [TestCase("L", "L")]
    [TestCase(null, "")]
    public void TheOrb_CarriesTheAgentsMark(string? mark, string shown)
    {
        using var ctx = new BunitContext();

        var orb = ctx.Render<LouisAgent.Web.Components.Widget.Orb.Orb>(parameters => parameters
            .Add(o => o.Label, "the guide")
            .Add(o => o.Mark, mark)).Find(".orb");

        Assert.That(orb.TextContent.Trim(), Is.EqualTo(shown));
    }

    /// <summary>
    /// The orb's rings keep close to it in every state: breathing and the rings of showing grow only a little, so
    /// they stay inside whatever holds the orb and never cross its edge.
    /// </summary>
    [Test]
    public void TheOrbsRings_KeepClose()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Styles, Does.Contain("inset: -3px;"));
            Assert.That(Styles, Does.Contain("@keyframes orb-breathe { 50% { transform: scale(1.08);"));
            Assert.That(Styles, Does.Contain("to { transform: scale(1.15); opacity: 0; }"));
        });
    }

    /// <summary>
    /// The disc is the theme's mark (F151.4, F151.6): the surface tool lays the theme's drawing of the mark over the
    /// mark's fill, with its gloss and ink, so it is the design's ball of glass in one theme and a flat disc in
    /// another, and the orb's sheet names no colour of its own for it.
    /// </summary>
    [Test]
    public void TheDisc_IsTheThemesDrawing_OverTheMarksThreeNames()
    {
        string tool = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "src/web/LouisAgent.Web/Styles/web/tools/surface/_marks.scss")).ReplaceLineEndings("\n");
        int start = tool.IndexOf("@mixin mark()", StringComparison.Ordinal);
        string mark = start < 0 ? "" : tool[start..tool.IndexOf("\n}", start, StringComparison.Ordinal)];

        Assert.Multiple(() =>
        {
            Assert.That(mark, Does.Contain("@include drawn(mark, var(--web-mark-fill));")
                .And.Contain("var(--web-mark-gloss)").And.Contain("var(--web-mark-ink)"));
            Assert.That(Styles, Does.Contain("@include surface.mark();"));
        });
    }

    /// <summary>While it listens, the ring round the disc is in the visitor's colour, since it is them it hears.</summary>
    [Test]
    public void Listening_TheRingIsInTheVisitorsColour() =>
        Assert.That(Styles, Does.Contain(".orb.is-listening::before { @include surface.mark-ring(var(--web-you)"));

    /// <summary>
    /// The rule: a place just shown is showing, whatever else; streaming with no words yet is thinking; words arriving
    /// is speaking. Only when the agent is doing none of those does it listen, while the visitor writes or holds a
    /// piece of the page; and the rest is idle.
    /// </summary>
    [TestCase(false, false, false, false, OrbState.Idle)]
    [TestCase(false, false, false, true, OrbState.Listening)]
    [TestCase(true, false, false, false, OrbState.Thinking)]
    [TestCase(true, false, false, true, OrbState.Thinking)]
    [TestCase(true, true, false, true, OrbState.Speaking)]
    [TestCase(true, true, true, false, OrbState.Showing)]
    [TestCase(false, false, true, true, OrbState.Showing)]
    public void TheRule_ReadsTheChatsState(bool streaming, bool hasWords, bool showing, bool heard,
        OrbState expected)
    {
        var facts = new OrbFacts(streaming, hasWords, showing, heard);

        Assert.That(OrbStates.Of(facts), Is.EqualTo(expected));
    }
}
