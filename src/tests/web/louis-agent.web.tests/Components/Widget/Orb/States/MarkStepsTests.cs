using LouisAgent.Web.Components.Widget.Orb;
using LouisAgent.Web.Components.Widget.Orb.States;

namespace LouisAgent.Web.Tests.Components.Widget.Orb.States;

/// <summary>
/// The states the landing's mark plays (F151.6): four of them, in the order an answer goes through them, each with a
/// name a visitor reads and a line that says what the mark does in it.
/// </summary>
public class MarkStepsTests
{
    /// <summary>Resting, listening, thinking, answering, each one a state the framework's orb draws.</summary>
    [Test]
    public void TheSteps_AreTheFour_InAnAnswersOrder()
    {
        Assert.Multiple(() =>
        {
            Assert.That(MarkSteps.All.Select(step => step.Name),
                Is.EqualTo(new[] { "Resting", "Listening", "Thinking", "Answering" }));
            Assert.That(MarkSteps.All.Select(step => step.State),
                Is.EqualTo(new[] { OrbState.Idle, OrbState.Listening, OrbState.Thinking, OrbState.Speaking }));
            Assert.That(MarkSteps.All.Select(step => step.Line), Has.All.Not.Empty.And.All.EndsWith("."));
        });
    }

    /// <summary>After each step comes the next, and after the last the first again.</summary>
    [TestCase(0, 1)]
    [TestCase(2, 3)]
    [TestCase(3, 0)]
    public void AfterAStep_ComesTheNext_AndRoundAgain(int at, int next)
    {
        Assert.That(MarkSteps.After(at), Is.EqualTo(next));
    }
}
