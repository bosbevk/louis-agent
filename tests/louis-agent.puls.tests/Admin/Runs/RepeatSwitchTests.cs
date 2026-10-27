using LouisAgent.Testing;
using LouisAgent.Puls.Admin.Runs;

namespace LouisAgent.Puls.Tests.Admin.Runs;

// 6.0b: which level the run explorer shows after another repeat is chosen (2026-10-26: "a repeat keeps the open conversation"), out of the razor page.
// Written first: the rule was in RunExplorer's @code. The conversations are the new repeat's, across the modes, as a task and its turn numbers.
public class RepeatSwitchTests
{
    private static readonly (string Task, IEnumerable<int> Turns)[] Repeat = [(SampleScenario.Alpha, [1, 2]), (SampleScenario.Beta, [1])];

    [Test]
    public void TheOpenConversation_StaysOpen_WhenTheNewRepeatHasIt()
    {
        Assert.That(RepeatSwitch.LevelAfter(2, SampleScenario.Alpha, null, Repeat), Is.EqualTo(2));
        Assert.That(RepeatSwitch.LevelAfter(3, SampleScenario.Alpha, 2, Repeat), Is.EqualTo(3));
    }

    [Test]
    public void ARequest_GoesUpToItsTurn_BecauseTheNewRepeatHasOtherRequests()
    {
        Assert.That(RepeatSwitch.LevelAfter(4, SampleScenario.Alpha, 1, Repeat), Is.EqualTo(3));
    }

    [Test]
    public void ATaskOrTurnTheNewRepeatLacks_GoesBackToTheOverview()
    {
        Assert.That(RepeatSwitch.LevelAfter(2, "cook-nl", null, Repeat), Is.Zero, "no such conversation");
        Assert.That(RepeatSwitch.LevelAfter(3, SampleScenario.Beta, 2, Repeat), Is.Zero, "the conversation has no turn 2");
    }

    [Test]
    public void NoOpenConversation_IsTheOverview()
    {
        Assert.That(RepeatSwitch.LevelAfter(0, null, null, Repeat), Is.Zero);
        Assert.That(RepeatSwitch.LevelAfter(1, null, null, Repeat), Is.Zero, "the conversations list has no task to keep");
    }
}
