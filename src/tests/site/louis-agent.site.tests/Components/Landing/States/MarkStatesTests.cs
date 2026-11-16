using LouisAgent.Site.Components.Landing.States;

namespace LouisAgent.Site.Tests.Components.Landing.States;

/// <summary>
/// The mark at the top of the landing (F151.6, Louis's pick: the mockup's top): the agent's mark drawn large, the
/// name of the state it is in with a line that says what it means, and a button for each state. It plays them in
/// turn until the visitor presses one, and then it holds that one.
/// </summary>
public class MarkStatesTests
{
    /// <summary>It opens resting: the mark idle, the first button the current one, and nothing held.</summary>
    [Test]
    public void ItOpens_Resting_WithAButtonForEachState()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<MarkStates>();

        var buttons = cut.FindAll(".mark-steps button");
        Assert.Multiple(() =>
        {
            Assert.That(cut.Find(".mark-states .orb").ClassList, Does.Contain("is-idle"));
            Assert.That(cut.Find(".mark-say b").TextContent, Is.EqualTo("Resting"));
            Assert.That(cut.Find(".mark-say span").TextContent, Is.EqualTo(MarkSteps.All[0].Line));
            Assert.That(buttons.Select(button => button.TextContent.Trim()),
                Is.EqualTo(new[] { "Resting", "Listening", "Thinking", "Answering" }));
            Assert.That(buttons.Select(button => button.GetAttribute("aria-current")),
                Is.EqualTo(new[] { "true", "false", "false", "false" }));
            Assert.That(cut.Find(".mark-steps").ClassList, Does.Not.Contain("is-held"));
        });
    }

    /// <summary>A press shows that state and holds it: the mark, the words and the current button all follow.</summary>
    [Test]
    public void APress_ShowsThatState_AndHoldsIt()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<MarkStates>();

        cut.FindAll(".mark-steps button")[2].Click();

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find(".mark-states .orb").ClassList, Does.Contain("is-thinking"));
            Assert.That(cut.Find(".mark-say b").TextContent, Is.EqualTo("Thinking"));
            Assert.That(cut.FindAll(".mark-steps button")[2].GetAttribute("aria-current"), Is.EqualTo("true"));
            Assert.That(cut.Find(".mark-steps").ClassList, Does.Contain("is-held"));
        });
    }
}
