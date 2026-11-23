using LouisAgent.Composition.Messages;
using LouisAgent.Composition.Packs.Sequences;
using LouisAgent.Core.Delegation.Messages;
using LouisAgent.Core.Delegation.Messages.Holes.Sequences;

namespace LouisAgent.Core.Delegation.Sequences;

/// <summary>
/// What the next step of a sequence is sent, as the sequence's <see cref="StepInput"/> says. Until a step has
/// answered, that is the first message: the first step always gets it.
/// </summary>
/// <remarks>
/// A switch expression on the enum, one arm per value, read like a table (ADR 0051). Each value is a line or two of
/// text and is switched on here only, so a Strategy class per value would be three classes for three lines; Fowler's
/// Replace Conditional with Polymorphism pays when the same switch repeats, and this one doesn't.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0117-one-address-for-every-agent-call.md">ADR 0117: one address for every agent call</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0051-flat-conditionals-and-named-endpoints.md">ADR 0051: flat conditionals</seealso>
internal static class StepMessage
{
    /// <summary>The message for the next step, from the first message and the steps done so far.</summary>
    public static string For(StepInput input, string firstMessage, IReadOnlyList<StepOutcome> done)
    {
        // A step that failed and was left out has no answer to pass on.
        var answered = done.Where(step => !step.Failed).ToList();
        if (answered.Count == 0) return firstMessage;

        return input switch
        {
            StepInput.FirstMessageAndPreviousAnswer => WithPrevious(firstMessage, answered[^1]),
            StepInput.WholeConversation => WithEvery(firstMessage, answered),
            StepInput.PreviousAnswer => answered[^1].Answer,
            _ => throw new ArgumentOutOfRangeException(nameof(input), input, null)
        };
    }

    /// <summary>The first message, then every answer so far in order, each under the step that gave it.</summary>
    private static string WithEvery(string firstMessage, IReadOnlyList<StepOutcome> answered)
    {
        var answers = string.Join("\n\n", answered.Select(Answered));
        var input = new SequenceStepsSoFarHoles(firstMessage, answers);
        return MessageText.Format(SubSessionToolMessage.SequenceStepsSoFar, input);
    }

    /// <summary>One answer under the step that gave it.</summary>
    private static string Answered(StepOutcome step)
    {
        var answered = new SequenceStepAnsweredHoles(step.Address, step.Answer);
        return MessageText.Format(SubSessionToolMessage.SequenceStepAnswered, answered);
    }

    /// <summary>The first message with the answer of the step before under it.</summary>
    private static string WithPrevious(string firstMessage, StepOutcome previous)
    {
        var input = new SequenceStepInputHoles(firstMessage, previous.Address, previous.Answer);
        return MessageText.Format(SubSessionToolMessage.SequenceStepInput, input);
    }
}
