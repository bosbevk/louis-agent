using LouisAgent.Composition.Messages;
using LouisAgent.Core.Delegation.Messages;
using LouisAgent.Core.Delegation.Messages.Holes.Sequences.Answers;

namespace LouisAgent.Core.Delegation.Sequences;

/// <summary>
/// How one step of a sequence went: the agent's answer and what it did, or why it failed.
/// </summary>
/// <param name="Number">Where the step stands in the sequence, from 1.</param>
/// <param name="Address">The step's address, as the identity wrote it.</param>
/// <param name="Answer">What the agent answered; empty when it failed.</param>
/// <param name="Failure">Why it failed, as the model would read it; none when it answered.</param>
/// <param name="Did">What it did, from its tool calls; empty when it failed.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0117-one-address-for-every-agent-call.md">ADR 0117: one address for every agent call</seealso>
internal sealed record StepOutcome(int Number, string Address, string Answer, string? Failure, string Did)
{
    /// <summary>Whether the step failed, so it has no answer to pass on.</summary>
    public bool Failed => Failure is not null;

    /// <summary>A step that answered.</summary>
    public static StepOutcome Answered(int number, string address, string answer, string did) =>
        new(number, address, answer, null, did);

    /// <summary>A step that failed, with the reason.</summary>
    public static StepOutcome Refused(int number, string address, string why) => new(number, address, "", why, "");

    /// <summary>
    /// The step in the answer's line: its place, its address and what it did, or, left out, why it failed.
    /// </summary>
    public string DidLine()
    {
        if (Failed)
        {
            var skipped = new SequenceStepSkippedHoles(Number, Address, Failure!);
            return MessageText.Format(SubSessionToolMessage.SequenceStepSkipped, skipped);
        }

        var did = new SequenceStepDidHoles(Number, Address, Did);
        return MessageText.Format(SubSessionToolMessage.SequenceStepDid, did);
    }
}
