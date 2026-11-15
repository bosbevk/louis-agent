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
