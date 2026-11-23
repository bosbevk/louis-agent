using LouisAgent.Composition.Messages;
using LouisAgent.Composition.Packs.Sequences;
using LouisAgent.Core.Delegation.Messages;
using LouisAgent.Core.Delegation.Messages.Holes.Sequences.Answers;

namespace LouisAgent.Core.Delegation.Sequences;

/// <summary>
/// What the asker's model gets back from a sequence, built in code: the last step's answer with a line under it that
/// names each step and what it did, or, when it stopped, where, why, and which steps finished first.
/// </summary>
/// <remarks>
/// The line follows the footer under one asked agent's answer (<c>[Answered by …, did: …]</c>), so a model that
/// reads one reads the other. It names what each step did, never what it used, so a replay still matches (FX27).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0117-one-address-for-every-agent-call.md">ADR 0117: one address for every agent call</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
internal static class SequenceAnswer
{
    /// <summary>
    /// The last answer, under the line naming each step, a step left out among them; with every step left out, the
    /// reason there is no answer.
    /// </summary>
    /// <remarks>Guard clause (Fowler): no answer first, then the last one given.</remarks>
    public static string Finished(AgentSequence sequence, IReadOnlyList<StepOutcome> done)
    {
        if (done.LastOrDefault(step => !step.Failed) is not { } last)
        {
            var none = new SequenceNoStepAnsweredHoles(sequence.Name, Steps(done));
            return MessageText.Format(SubSessionToolMessage.SequenceNoStepAnswered, none);
        }

        var answer = new SequenceAnswerHoles(last.Answer, sequence.Name, Steps(done));
        return MessageText.Format(SubSessionToolMessage.SequenceAnswer, answer);
    }

    /// <summary>Where the sequence stopped and why, with the steps that finished before it.</summary>
    public static string Stopped(AgentSequence sequence, StepOutcome failed, IReadOnlyList<StepOutcome> done)
    {
        var stopped = new SequenceStoppedHoles(sequence.Name, failed.Number, sequence.Steps.Count, failed.Address,
            failed.Failure ?? "", done.Count == 0 ? "(none)" : Steps(done));
        return MessageText.Format(SubSessionToolMessage.SequenceStopped, stopped);
    }

    /// <summary>Each step in order, with what it did.</summary>
    private static string Steps(IReadOnlyList<StepOutcome> done) =>
        string.Join("; ", done.Select(step => step.DidLine()));
}
