namespace LouisAgent.Web.Evals.Tests.Pages.Flips;

/// <summary>F126.4: a run's page tags a conversation that passed only after a person's flip.</summary>
public class RunPageFlipTests
{
    /// <summary>A conversation that passed after a flip is tagged so on its row; one that failed is not.</summary>
    [Test]
    public void AConversationThatPassedAfterAFlip_IsTaggedSoOnItsRow()
    {
        using var ctx = new BunitContext();
        using var other = new BunitContext();

        var flipped = new EvalPagesHost(ConversationFlipTests.Flipped()).Render(ctx, "guide", "20261123-131900-guide");
        var failed = new EvalPagesHost(ConversationFlipTests.Run()).Render(other, "guide", "20261123-131900-guide");

        Assert.That(flipped.Find(".conversation-row-flipped").TextContent, Is.EqualTo("Passed after your flip"));
        Assert.That(failed.FindAll(".conversation-row-flipped"), Is.Empty);
    }
}
