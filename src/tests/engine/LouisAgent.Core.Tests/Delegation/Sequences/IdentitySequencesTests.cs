using LouisAgent.Composition.Packs.Sequences;
using LouisAgent.Core.Delegation.Sequences;

namespace LouisAgent.Core.Tests.Delegation.Sequences;

/// <summary>
/// The rules a sequence keeps, checked in code where it is read, each with its reason: at least two steps, every step
/// an address in the identity's <c>Calls</c>, no address twice in one sequence, an address in one sequence only, and
/// every step an agent, never a lookup agent.
/// </summary>
public class IdentitySequencesTests
{
    /// <summary>Whom the identity may call: a voice, a subagent and a lookup agent here, a shop on the partner.</summary>
    private static readonly string[] Calls = ["testprod", "testprod.researcher", "testprod.lookup", "partner:shop"];

    /// <summary>Why the first of the sequences given can't run, as an identity that declares them all.</summary>
    private static string? RefusalOf(params AgentSequence[] sequences)
    {
        var identity = new FakeIdentity("triage", calls: Calls, sequences: sequences);
        return IdentitySequences.Of(identity).RefusalOf(sequences[0]);
    }

    /// <summary>A sequence that keeps every rule runs, here and on another server.</summary>
    [Test]
    public void ASequenceThatKeepsEveryRule_IsNotRefused() =>
        Assert.That(RefusalOf(new AgentSequence("fix", ["testprod", "testprod.researcher", "partner:shop"])), Is.Null);

    /// <summary>One step is no sequence: it is one ask.</summary>
    [Test]
    public void OneStep_IsRefused() =>
        Assert.That(RefusalOf(new AgentSequence("fix", ["testprod"])), Is.EqualTo(
            "Error: the sequence fix can't run: a sequence needs at least 2 steps, and it has 1."));

    /// <summary>A step the identity may not call, or that is no address at all, is refused.</summary>
    [TestCase("testprod.cook")]
    [TestCase("not an address")]
    public void AStepNotInTheCalls_IsRefused(string step) =>
        Assert.That(RefusalOf(new AgentSequence("fix", ["testprod", step])), Is.EqualTo(
            $"Error: the sequence fix can't run: its step '{step}' is no address in the Calls of the agent " +
            "that owns it."));

    /// <summary>The same address twice in one sequence is refused, however it is cased.</summary>
    [Test]
    public void TheSameAddressTwice_IsRefused() =>
        Assert.That(RefusalOf(new AgentSequence("fix", ["testprod", "testprod.researcher", "TestProd"])),
            Is.EqualTo("Error: the sequence fix can't run: it asks 'TestProd' twice."));

    /// <summary>
    /// An address in two sequences of one identity is refused, so which sequence an ask starts is never in doubt.
    /// </summary>
    [Test]
    public void AnAddressInTwoSequences_IsRefused() =>
        Assert.That(RefusalOf(new AgentSequence("fix", ["testprod", "testprod.researcher"]),
                new AgentSequence("review", ["partner:shop", "testprod.researcher"])),
            Is.EqualTo("Error: the sequence fix can't run: its step 'testprod.researcher' is in the sequence " +
                       "review too, and an address belongs to one sequence only."));

    /// <summary>A lookup agent works with one of the asker's tools, so it is no step.</summary>
    [Test]
    public void ALookupAgent_IsRefused() =>
        Assert.That(RefusalOf(new AgentSequence("fix", ["testprod", "testprod.lookup"])), Is.EqualTo(
            "Error: the sequence fix can't run: its step 'testprod.lookup' is a lookup agent, and a step is an " +
            "agent: an identity's voice or one of its subagents."));
}
