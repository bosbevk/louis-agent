using LouisAgent.Composition.Packs;
using LouisAgent.Composition.Packs.Sequences;

namespace LouisAgent.Core.Tests.Composition.Packs.Sequences;

/// <summary>
/// A sequence is optional, and says only what it changes: an identity that declares none has none, and a sequence
/// that names no choices gives each step the first message with the answer before, and stops where a step fails.
/// </summary>
public class AgentSequenceTests
{
    /// <summary>An identity that says nothing about sequences runs none, so it decides whom to ask as before.</summary>
    [Test]
    public void AnIdentityThatDeclaresNone_HasNoSequences()
    {
        IAgentIdentity identity = new PlainIdentity();

        Assert.That(identity.Sequences, Is.Empty);
    }

    /// <summary>
    /// A sequence that names only its steps gives each step the first message and the answer before, and stops at a
    /// failed step.
    /// </summary>
    [Test]
    public void ASequenceThatNamesOnlyItsSteps_TakesTheDefaults()
    {
        var sequence = new AgentSequence("fix-an-error", ["triage.reader", "triage.fixer"]);

        Assert.Multiple(() =>
        {
            Assert.That(sequence.Input, Is.EqualTo(StepInput.FirstMessageAndPreviousAnswer));
            Assert.That(sequence.Failure, Is.EqualTo(StepFailure.Stop));
            Assert.That(sequence.Steps, Is.EqualTo(new[] { "triage.reader", "triage.fixer" }));
        });
    }

    /// <summary>An identity that sets only what it must, so every other member keeps its default.</summary>
    private sealed class PlainIdentity : IAgentIdentity
    {
        /// <summary>Its name.</summary>
        public string Name => "plain";

        /// <summary>No prompts: the test never reads them.</summary>
        public Microsoft.Extensions.FileProviders.IFileProvider Prompts { get; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();

        /// <summary>No packs.</summary>
        public IReadOnlyList<string> Packs => [];
    }
}
