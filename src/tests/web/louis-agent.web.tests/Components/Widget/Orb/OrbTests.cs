using LouisAgent.Web.Components.Widget.Orb;

namespace LouisAgent.Web.Tests.Components.Widget.Orb;

/// <summary>
/// F134.3: the agent's orb shows what the agent is doing, in four states the J and K mockups drew. Idle it breathes,
/// thinking an arc goes round, showing rings go out to the page, speaking it pulses. The state is read from the
/// chat's own state by a rule, never guessed.
/// </summary>
public class OrbTests
{
    /// <summary>The orb wears its state as a class the styles read, and a screen reader hears it.</summary>
    [TestCase(OrbState.Idle, "orb is-idle")]
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
        string styles = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "src/web/louis-agent.web/Components/Widget/Orb/Orb.razor.scss")).ReplaceLineEndings("\n");

        Assert.Multiple(() =>
        {
            Assert.That(styles, Does.Contain("inset: -5px;"));
            Assert.That(styles, Does.Contain("@keyframes orb-breathe { 50% { transform: scale(1.08);"));
            Assert.That(styles, Does.Contain("to { transform: scale(1.15); opacity: 0; }"));
        });
    }

    /// <summary>
    /// The rule: a place just shown is showing, whatever else; streaming with no words yet is thinking; words arriving
    /// is speaking; and the rest is idle.
    /// </summary>
    [TestCase(false, false, false, OrbState.Idle)]
    [TestCase(true, false, false, OrbState.Thinking)]
    [TestCase(true, true, false, OrbState.Speaking)]
    [TestCase(true, true, true, OrbState.Showing)]
    [TestCase(false, false, true, OrbState.Showing)]
    public void TheRule_ReadsTheChatsState(bool streaming, bool hasWords, bool showing, OrbState expected) =>
        Assert.That(OrbStates.Of(streaming, hasWords, showing), Is.EqualTo(expected));
}
