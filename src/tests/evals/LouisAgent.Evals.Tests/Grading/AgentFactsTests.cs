using LouisAgent.Core.Delegation.Agents;
using LouisAgent.Evals.Files;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Transcripts;

namespace LouisAgent.Evals.Tests.Grading;

/// <summary>
/// The facts that follow another agent (F126.2 step 9): <c>routed-to</c> reads this turn's asks of other agents, and
/// <c>continued</c> wants the same agent asked in this turn and in an earlier one.
/// </summary>
public class AgentFactsTests
{
    /// <summary>The guest house's agent, as the showroom names it.</summary>
    private const string Guesthouse = "seemeeu-guesthouse";

    /// <summary>A call that asks the agent at this address, as the engine's tool writes its input.</summary>
    private static ToolCallTranscript Ask(string address) =>
        new(nameof(AskAgentTools.AskAgent), $$"""{"address":"{{address}}","message":"Is there a room?"}""", "Yes.");

    /// <summary>A turn with these calls.</summary>
    private static TurnTranscript Turn(params ToolCallTranscript[] calls) =>
        new("Book me a room.", "Done.", calls, TimeSpan.FromSeconds(5));

    /// <summary>The fact's mark for a turn, with the turns before it.</summary>
    private static Mark Tell(string fact, TurnTranscript turn, params TurnTranscript[] earlier) =>
        Facts.Tell(Fact.Parse(fact), turn, earlier);

    /// <summary>routed-to is met when this turn asked that address, however its letters are cased.</summary>
    [Test]
    public void RoutedTo_IsMet_WhenThisTurnAskedTheAddress()
    {
        var mark = Tell($"routed-to {Guesthouse}", Turn(Ask("SeeMeeU-Guesthouse")));

        Assert.Multiple(() =>
        {
            Assert.That(mark.Met, Is.True);
            Assert.That(mark.Reason, Does.Contain(Guesthouse));
        });
    }

    /// <summary>It isn't met when the turn asked another agent, and the reason names who was asked.</summary>
    [Test]
    public void RoutedTo_IsNotMet_WhenTheTurnAskedAnother()
    {
        var mark = Tell($"routed-to {Guesthouse}", Turn(Ask("acme.cook")));

        Assert.Multiple(() =>
        {
            Assert.That(mark.Met, Is.False);
            Assert.That(mark.Reason, Does.Contain("acme.cook"));
        });
    }

    /// <summary>It isn't met when the turn asked nobody.</summary>
    [Test]
    public void RoutedTo_IsNotMet_WhenNobodyWasAsked() =>
        Assert.That(Tell($"routed-to {Guesthouse}", Turn()).Met, Is.False);

    /// <summary>continued is met when this turn and an earlier one asked the address.</summary>
    [Test]
    public void Continued_IsMet_WhenAskedNowAndBefore() =>
        Assert.That(Tell($"continued {Guesthouse}", Turn(Ask(Guesthouse)), Turn(Ask(Guesthouse))).Met, Is.True);

    /// <summary>On a first turn it is never met, nor when only an earlier turn asked.</summary>
    [Test]
    public void Continued_IsNotMet_OnAFirstTurn_OrWhenOnlyBefore()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Tell($"continued {Guesthouse}", Turn(Ask(Guesthouse))).Met, Is.False);
            Assert.That(Tell($"continued {Guesthouse}", Turn(), Turn(Ask(Guesthouse))).Met, Is.False);
        });
    }

    /// <summary>An ask whose input holds no address that parses counts as asking nobody.</summary>
    [TestCase("""{"message":"Hi"}""")]
    [TestCase("""{"address":"Not An Address"}""")]
    [TestCase("not json")]
    public void AnAskWithNoAddress_AsksNobody(string input)
    {
        var broken = new ToolCallTranscript(nameof(AskAgentTools.AskAgent), input, "Failed.", Failed: true);

        Assert.That(Tell($"routed-to {Guesthouse}", Turn(broken)).Met, Is.False);
    }
}
