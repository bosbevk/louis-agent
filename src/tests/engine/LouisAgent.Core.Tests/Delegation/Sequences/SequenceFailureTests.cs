using LouisAgent.Composition.Packs;
using LouisAgent.Composition.Packs.Sequences;
using LouisAgent.Composition.Sessions;
using LouisAgent.Core.Delegation.Sequences;

namespace LouisAgent.Core.Tests.Delegation.Sequences;

/// <summary>
/// What a sequence does when a step fails, as its <see cref="StepFailure"/> says: ask that step once more, or leave it
/// out and carry on, naming it in the answer so it never goes unnoticed.
/// </summary>
public class SequenceFailureTests
{
    /// <summary>The steps of the sequence the tests run.</summary>
    private static readonly string[] ThreeSteps = ["testprod", "testprod.researcher", "partner:shop"];

    /// <summary>The identity that owns the sequence, which may call each of its steps.</summary>
    private static readonly FakeIdentity Triage = new("triage", calls: ThreeSteps);

    /// <summary>The identities this instance runs.</summary>
    private static readonly IReadOnlyList<IAgentIdentity> Identities =
        [Triage, new FakeIdentity("testprod", voice: "testprod/voice-en")];

    /// <summary>Runs the three steps on the pretend servers, doing what the failure given says.</summary>
    private static async Task<string> RunAsync(SequencePeer peer, StepFailure failure)
    {
        using var _ = TurnIdentity.Use(TurnIdentity.Of(Triage));
        var runner = new SequenceRunner(SequencePeer.Servers, Identities, "own-key", new HttpClient(peer.Handler));
        var sequence = new AgentSequence("fix-an-error", ThreeSteps, Failure: failure);
        return await runner.RunAsync(sequence, "The build fails on main.");
    }

    /// <summary>
    /// Asked once more, a step that failed and then answers carries the sequence on: the same message goes to a new
    /// conversation, and the last answer comes back.
    /// </summary>
    [Test]
    public async Task RetryOnce_AStepThatAnswersTheSecondTime_CarriesOn()
    {
        var peer = new SequencePeer();
        peer.Failing.Add(2);

        string answer = await RunAsync(peer, StepFailure.RetryOnce);

        Assert.Multiple(() =>
        {
            Assert.That(peer.Opened, Is.EqualTo(new[]
            {
                "self testprod", "self testprod.researcher", "self testprod.researcher", "partner shop"
            }));
            Assert.That(peer.Sent[2], Is.EqualTo(peer.Sent[1]), "the same message, asked again");
            Assert.That(answer, Does.StartWith("Answer 4\n\n[Answered by the sequence fix-an-error"));
        });
    }

    /// <summary>A step that fails the second time too stops the sequence there, as the default does.</summary>
    [Test]
    public async Task RetryOnce_AStepThatFailsTwice_StopsTheSequence()
    {
        var peer = new SequencePeer();
        peer.Failing.UnionWith([2, 3]);

        string answer = await RunAsync(peer, StepFailure.RetryOnce);

        Assert.Multiple(() =>
        {
            Assert.That(answer, Does.StartWith(
                "Error: the sequence fix-an-error stopped at step 2 of 3, testprod.researcher: "));
            Assert.That(peer.Opened, Has.Count.EqualTo(3), "asked twice, and the shop never");
        });
    }

    /// <summary>
    /// Skipped, a failed step is left out: the next step gets the answer before the failed one, and the answer's line
    /// names the step left out and why.
    /// </summary>
    [Test]
    public async Task Skip_AFailedStepIsLeftOut_AndNamedInTheAnswer()
    {
        var peer = new SequencePeer();
        peer.Failing.Add(2);

        string answer = await RunAsync(peer, StepFailure.Skip);

        Assert.Multiple(() =>
        {
            Assert.That(peer.Sent[2], Does.EndWith("The step before you, testprod, answered:\n\nAnswer 1"));
            Assert.That(answer, Does.StartWith("Answer 3\n\n[Answered by the sequence fix-an-error"));
            Assert.That(answer, Does.Contain("(2) testprod.researcher was skipped: Error:"));
            Assert.That(answer, Does.Contain("(3) partner:shop did: no tools]"));
        });
    }

    /// <summary>When every step is left out there is no answer to give, and the reply says so, naming each.</summary>
    [Test]
    public async Task Skip_EveryStepFailing_SaysNoStepAnswered()
    {
        var peer = new SequencePeer();
        peer.Failing.UnionWith([1, 2, 3]);

        string answer = await RunAsync(peer, StepFailure.Skip);

        Assert.Multiple(() =>
        {
            Assert.That(answer, Does.StartWith("Error: no step of the sequence fix-an-error answered: "));
            Assert.That(answer, Does.Contain("(1) testprod was skipped").And.Contain("(3) partner:shop was skipped"));
        });
    }
}
