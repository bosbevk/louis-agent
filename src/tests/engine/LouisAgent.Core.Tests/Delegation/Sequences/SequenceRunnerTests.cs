using LouisAgent.Composition.Packs;
using LouisAgent.Composition.Packs.Sequences;
using LouisAgent.Composition.Sessions;
using LouisAgent.Core.Delegation.Sequences;
using LouisAgent.Core.Sessions;

namespace LouisAgent.Core.Tests.Delegation.Sequences;

/// <summary>
/// A sequence asks its steps in the order the identity wrote them, each held to the rules of one ask: by default each
/// step after the first gets the first message and the answer before it, and the sequence stops at a failed step,
/// saying why and which steps finished.
/// </summary>
public class SequenceRunnerTests
{
    /// <summary>The steps of the sequence the tests run: a voice and a subagent here, a shop on the partner.</summary>
    private static readonly string[] ThreeSteps = ["testprod", "testprod.researcher", "partner:shop"];

    /// <summary>The identity that owns the sequence, which may call each of its steps.</summary>
    private static readonly FakeIdentity Triage = new("triage", calls: ThreeSteps);

    /// <summary>The identities this instance runs.</summary>
    private static readonly IReadOnlyList<IAgentIdentity> Identities =
        [Triage, new FakeIdentity("testprod", voice: "testprod/voice-en")];

    /// <summary>Runs the sequence as the owner's turn, on the pretend servers.</summary>
    private static async Task<string> RunAsync(SequencePeer peer, AgentSequence sequence, IAgentIdentity? owner = null,
        int maxDepth = 4)
    {
        // The owner the test gives runs on this instance in place of the usual one.
        IReadOnlyList<IAgentIdentity> identities = owner is null ? Identities : [owner, Identities[1]];
        using var _ = TurnIdentity.Use(TurnIdentity.Of(owner ?? Triage));
        var runner = new SequenceRunner(SequencePeer.Servers, identities, "own-key", new HttpClient(peer.Handler),
            maxDepth: maxDepth);
        return await runner.RunAsync(sequence, "The build fails on main.");
    }

    /// <summary>
    /// Every step answers: they are opened in order, each where its address says, and the last answer comes back
    /// under a line naming each step and what it did.
    /// </summary>
    [Test]
    public async Task EveryStepAnswers_InOrder_AndTheLastAnswerComesBackUnderALineNamingEach()
    {
        var peer = new SequencePeer();

        string answer = await RunAsync(peer, new AgentSequence("fix-an-error", ThreeSteps));

        Assert.Multiple(() =>
        {
            Assert.That(peer.Opened,
                Is.EqualTo(new[] { "self testprod", "self testprod.researcher", "partner shop" }));
            Assert.That(answer, Does.StartWith("Answer 3\n\n[Answered by the sequence fix-an-error, in this order: "));
            Assert.That(answer, Does.Contain(
                "(1) testprod did: no tools; (2) testprod.researcher did: no tools; (3) partner:shop did: no tools]"));
        });
    }

    /// <summary>
    /// By default the first step gets the first message, and each after it the first message with the answer before.
    /// </summary>
    [Test]
    public async Task ByDefault_EachStepGetsTheFirstMessage_AndTheAnswerBefore()
    {
        var peer = new SequencePeer();

        await RunAsync(peer, new AgentSequence("fix-an-error", ThreeSteps));

        Assert.Multiple(() =>
        {
            Assert.That(peer.Sent[0], Is.EqualTo("The build fails on main."));
            Assert.That(peer.Sent[1], Is.EqualTo(
                "The build fails on main.\n\nThe step before you, testprod, answered:\n\nAnswer 1"));
            Assert.That(peer.Sent[2], Is.EqualTo(
                "The build fails on main.\n\nThe step before you, testprod.researcher, answered:\n\nAnswer 2"));
        });
    }

    /// <summary>
    /// By default a step that fails stops the sequence there: the answer says where and why, and names the steps that
    /// finished; no step after it is asked.
    /// </summary>
    [Test]
    public async Task ByDefault_AFailedStep_StopsTheSequence_SayingWhyAndWhichFinished()
    {
        var peer = new SequencePeer();
        peer.Failing.Add(2);

        string answer = await RunAsync(peer, new AgentSequence("fix-an-error", ThreeSteps));

        Assert.Multiple(() =>
        {
            Assert.That(answer, Does.StartWith(
                "Error: the sequence fix-an-error stopped at step 2 of 3, testprod.researcher: "));
            Assert.That(answer, Does.Contain("the model refused"));
            Assert.That(answer, Does.EndWith("Finished before it: (1) testprod did: no tools."));
            Assert.That(peer.Opened, Has.Count.EqualTo(2), "the shop is never asked");
        });
    }

    /// <summary>
    /// A sequence that breaks a rule, such as a step the owner doesn't name in its <c>Calls</c>, is refused in code
    /// before any step is opened.
    /// </summary>
    [Test]
    public async Task ASequenceThatBreaksARule_IsRefused_BeforeAnyStepIsOpened()
    {
        var peer = new SequencePeer();
        var owner = new FakeIdentity("triage", calls: ["testprod"]);

        string answer = await RunAsync(peer, new AgentSequence("fix-an-error", ["testprod", "testprod.researcher"]),
            owner);

        Assert.Multiple(() =>
        {
            Assert.That(answer, Does.StartWith("Error: the sequence fix-an-error can't run: its step " +
                                               "'testprod.researcher' is no address in the Calls"));
            Assert.That(peer.Opened, Is.Empty);
        });
    }

    /// <summary>
    /// A sequence already in the chain of asks is refused, so it can never contain itself; a step already in the
    /// chain is refused as one ask would be.
    /// </summary>
    [Test]
    public async Task ASequenceOrAStepAlreadyInTheChain_IsRefused()
    {
        var peer = new SequencePeer();
        var sequence = new AgentSequence("fix-an-error", ThreeSteps);

        string again, stepInChain;
        using (TurnChain.Use(["guide", "triage.fix-an-error"]))
            again = await RunAsync(peer, sequence);
        using (TurnChain.Use(["guide", "testprod"]))
            stepInChain = await RunAsync(peer, sequence);

        Assert.Multiple(() =>
        {
            Assert.That(again, Does.StartWith("Error: triage.fix-an-error is already working on this"));
            Assert.That(stepInChain, Does.Contain("stopped at step 1 of 3, testprod: Error: testprod is already"));
            Assert.That(stepInChain, Does.Contain("(guide > testprod > triage.fix-an-error)"));
            Assert.That(peer.Opened, Is.Empty);
        });
    }

    /// <summary>A step that would open an agent deeper than this instance allows is refused unopened.</summary>
    [Test]
    public async Task AStepPastTheDeepestLevel_IsRefused()
    {
        var peer = new SequencePeer();

        string answer;
        using (TurnDepth.Use(2))
            answer = await RunAsync(peer, new AgentSequence("fix-an-error", ThreeSteps), maxDepth: 2);

        Assert.Multiple(() =>
        {
            Assert.That(answer, Does.Contain("stopped at step 1 of 3").And.Contain("AGENT_MAX_DEPTH"));
            Assert.That(peer.Opened, Is.Empty);
        });
    }

    /// <summary>A turn that speaks for no identity has no sequence to run.</summary>
    [Test]
    public async Task ATurnWithNoIdentity_RunsNothing()
    {
        var peer = new SequencePeer();
        var runner = new SequenceRunner(SequencePeer.Servers, Identities, "own-key", new HttpClient(peer.Handler));

        string answer = await runner.RunAsync(new AgentSequence("fix-an-error", ThreeSteps), "The build fails.");

        Assert.Multiple(() =>
        {
            Assert.That(answer, Does.StartWith("Error: this chat speaks for no agent"));
            Assert.That(peer.Opened, Is.Empty);
        });
    }
}
