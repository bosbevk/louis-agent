using LouisAgent.Composition.Packs;
using LouisAgent.Composition.Packs.Sequences;
using LouisAgent.Composition.Sessions;
using LouisAgent.Core.Delegation.Sequences;

namespace LouisAgent.Core.Tests.Delegation.Sequences;

/// <summary>
/// What each step is given, as the sequence's <see cref="StepInput"/> says: the whole conversation so far, or only
/// the answer before. The first step always gets the first message.
/// </summary>
public class SequenceInputTests
{
    /// <summary>The steps of the sequence the tests run.</summary>
    private static readonly string[] ThreeSteps = ["testprod", "testprod.researcher", "partner:shop"];

    /// <summary>The identity that owns the sequence, which may call each of its steps.</summary>
    private static readonly FakeIdentity Triage = new("triage", calls: ThreeSteps);

    /// <summary>The identities this instance runs.</summary>
    private static readonly IReadOnlyList<IAgentIdentity> Identities =
        [Triage, new FakeIdentity("testprod", voice: "testprod/voice-en")];

    /// <summary>Runs the three steps with the input given, and gives back what each step was sent.</summary>
    private static async Task<IReadOnlyList<string>> SentAsync(StepInput input)
    {
        var peer = new SequencePeer();
        using var _ = TurnIdentity.Use(TurnIdentity.Of(Triage));
        var runner = new SequenceRunner(SequencePeer.Servers, Identities, "own-key", new HttpClient(peer.Handler));

        await runner.RunAsync(new AgentSequence("fix-an-error", ThreeSteps, input), "The build fails on main.");
        return peer.Sent;
    }

    /// <summary>
    /// The whole conversation: each step gets the first message, then every answer so far in order, each under the
    /// step that gave it.
    /// </summary>
    [Test]
    public async Task TheWholeConversation_GivesEachStepEveryAnswerSoFar()
    {
        var sent = await SentAsync(StepInput.WholeConversation);

        Assert.Multiple(() =>
        {
            Assert.That(sent[0], Is.EqualTo("The build fails on main."));
            Assert.That(sent[1], Is.EqualTo(
                "The build fails on main.\n\nWhat the steps before you answered, in order:\n\n" +
                "testprod answered:\nAnswer 1"));
            Assert.That(sent[2], Is.EqualTo(
                "The build fails on main.\n\nWhat the steps before you answered, in order:\n\n" +
                "testprod answered:\nAnswer 1\n\ntestprod.researcher answered:\nAnswer 2"));
        });
    }

    /// <summary>Only the previous answer: each step after the first gets the answer before, as it came back.</summary>
    [Test]
    public async Task OnlyThePreviousAnswer_GivesEachStepTheAnswerBefore()
    {
        var sent = await SentAsync(StepInput.PreviousAnswer);

        Assert.That(sent, Is.EqualTo(new[] { "The build fails on main.", "Answer 1", "Answer 2" }));
    }
}
