using LouisAgent.Composition.Packs;
using LouisAgent.Composition.Packs.Sequences;
using LouisAgent.Composition.Sessions;
using LouisAgent.Core.Delegation.Agents;
using LouisAgent.Core.Tests.Delegation.Sequences;
using LouisAgent.Core.Usage.Records;

namespace LouisAgent.Core.Tests.Delegation.Agents.Sequences;

/// <summary>
/// The engine forces a sequence on the ask: asking the first step of one of the asker's sequences runs the whole
/// sequence for one of the turn's asks, a later step is never asked on its own, and an address in no sequence is
/// asked as before.
/// </summary>
public class AskAgentSequenceTests
{
    /// <summary>The steps of the asker's sequence: a voice and a subagent here, then a shop on the partner.</summary>
    private static readonly string[] ThreeSteps = ["testprod", "testprod.researcher", "partner:shop"];

    /// <summary>The asker, which runs the sequence and may also ask the shop's bakery on its own.</summary>
    private static readonly FakeIdentity Triage = new("triage", calls: [.. ThreeSteps, "partner:bakery"],
        sequences: [new AgentSequence("fix-an-error", ThreeSteps)]);

    /// <summary>The identities this instance runs.</summary>
    private static readonly IReadOnlyList<IAgentIdentity> Identities =
        [Triage, new FakeIdentity("testprod", voice: "testprod/voice-en")];

    /// <summary>The tool on the pretend servers.</summary>
    private static AskAgentTools Tools(SequencePeer peer) =>
        new(SequencePeer.Servers, Identities, "own-key", new HttpClient(peer.Handler));

    /// <summary>Asks an address as the asker, with the tool's other arguments as given.</summary>
    private static async Task<string> AskAsync(AskAgentTools tools, string address, string? tool = null,
        bool stronger = false)
    {
        using var _ = TurnIdentity.Use(TurnIdentity.Of(Triage));
        return await tools.AskAgent(address, "The build fails on main.", tool: tool, stronger: stronger);
    }

    /// <summary>
    /// Asking the first step runs the whole sequence in its order, and the answer is the sequence's: the last step's
    /// answer under the line naming every step.
    /// </summary>
    [Test]
    public async Task AskingTheFirstStep_RunsTheWholeSequence()
    {
        var peer = new SequencePeer();

        string answer = await AskAsync(Tools(peer), "TestProd");

        Assert.Multiple(() =>
        {
            Assert.That(peer.Opened,
                Is.EqualTo(new[] { "self testprod", "self testprod.researcher", "partner shop" }));
            Assert.That(answer, Does.StartWith("Answer 3\n\n[Answered by the sequence fix-an-error, in this order: "));
        });
    }

    /// <summary>
    /// The sequence takes one of the turn's three asks, and its steps take none: two more asks go, a third is
    /// refused.
    /// </summary>
    [Test]
    public async Task ASequence_TakesOneOfTheTurnsAsks_ItsStepsNone()
    {
        var peer = new SequencePeer();
        var tools = Tools(peer);

        var answers = new List<string>();
        using (UsageScope.Begin(session: "sess_triage", turn: 1))
            foreach (string address in new[] { "testprod", "partner:bakery", "partner:bakery", "partner:bakery" })
                answers.Add(await AskAsync(tools, address));

        Assert.Multiple(() =>
        {
            Assert.That(answers.Take(3), Has.None.StartsWith("Error"));
            Assert.That(answers[3], Does.StartWith("Error: this turn has already asked other agents 3 times"));
        });
    }

    /// <summary>
    /// A later step is never asked on its own: the reason names the sequence and the address that starts it, and
    /// nothing is opened.
    /// </summary>
    [Test]
    public async Task ALaterStep_IsRefused_NamingTheAddressThatStartsIt()
    {
        var peer = new SequencePeer();

        string answer = await AskAsync(Tools(peer), "testprod.researcher");

        Assert.Multiple(() =>
        {
            Assert.That(answer, Is.EqualTo(
                "Error: testprod.researcher is step 2 of the sequence fix-an-error, so it is never asked on its " +
                "own. Ask testprod, which runs the whole sequence in order."));
            Assert.That(peer.Opened, Is.Empty);
        });
    }

    /// <summary>An ask that starts a sequence takes no tool and no stronger model, and runs nothing with one.</summary>
    [TestCase("FetchUrl", false)]
    [TestCase(null, true)]
    public async Task AToolOrAStrongerModel_OnAnAskThatStartsASequence_IsRefused(string? tool, bool stronger)
    {
        var peer = new SequencePeer();

        string answer = await AskAsync(Tools(peer), "testprod", tool, stronger);

        Assert.Multiple(() =>
        {
            Assert.That(answer, Is.EqualTo(
                "Error: testprod starts the sequence fix-an-error, which takes neither a tool nor a stronger " +
                "model. Ask it without them."));
            Assert.That(peer.Opened, Is.Empty);
        });
    }

    /// <summary>An address in no sequence is asked as before: one agent, under its own line.</summary>
    [Test]
    public async Task AnAddressInNoSequence_IsAskedAsBefore()
    {
        var peer = new SequencePeer();

        string answer = await AskAsync(Tools(peer), "partner:bakery");

        Assert.Multiple(() =>
        {
            Assert.That(peer.Opened, Is.EqualTo(new[] { "partner bakery" }));
            Assert.That(answer, Does.StartWith("Answer 1\n\n[Answered by partner:bakery"));
        });
    }
}
