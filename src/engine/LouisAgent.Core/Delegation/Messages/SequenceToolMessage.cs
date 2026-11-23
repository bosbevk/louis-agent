using System.ComponentModel;
using LouisAgent.Composition.Messages;
using LouisAgent.Core.Delegation.Messages.Holes.Sequences.Answers;
using LouisAgent.Core.Delegation.Messages.Holes.Sequences.Inputs;
using LouisAgent.Core.Delegation.Messages.Holes.Sequences.Rules;

namespace LouisAgent.Core.Delegation.Messages;

/// <summary>
/// What a sequence's steps are sent and what the asker's model gets back: the first message with the answers before,
/// the last answer under a line naming each step, where and why a sequence stopped, and why one can't run.
/// </summary>
/// <remarks>
/// Its own enum beside <see cref="SubSessionToolMessage"/>, which holds one ask's texts and has no room left under
/// the size limit. Like that enum's footers, a line names what each step did, never what it used, so a replay still
/// matches (FX27).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0117-one-address-for-every-agent-call.md">ADR 0117: one address for every agent call</seealso>
public enum SequenceToolMessage
{
    // What a step is sent.
    [Description("{FirstMessage}\n\nThe step before you, {Address}, answered:\n\n{Answer}")]
    [Holes(typeof(SequenceStepInputHoles))]
    SequenceStepInput,

    [Description("{FirstMessage}\n\nWhat the steps before you answered, in order:\n\n{Answers}")]
    [Holes(typeof(SequenceStepsSoFarHoles))]
    SequenceStepsSoFar,

    [Description("{Address} answered:\n{Answer}")]
    [Holes(typeof(SequenceStepAnsweredHoles))]
    SequenceStepAnswered,

    // What the asker gets back.
    [Description("({Number}) {Address} did: {Did}")]
    [Holes(typeof(SequenceStepDidHoles))]
    SequenceStepDid,

    [Description("({Number}) {Address} was skipped: {Reason}")]
    [Holes(typeof(SequenceStepSkippedHoles))]
    SequenceStepSkipped,

    [Description("Error: no step of the sequence {Sequence} answered: {Steps}")]
    [Holes(typeof(SequenceNoStepAnsweredHoles))]
    SequenceNoStepAnswered,

    [Description("{Answer}\n\n[Answered by the sequence {Sequence}, in this order: {Steps}]")]
    [Holes(typeof(SequenceAnswerHoles))]
    SequenceAnswer,

    [Description(
        "Error: the sequence {Sequence} stopped at step {Number} of {Count}, {Address}: {Reason} Finished before " +
        "it: {Finished}.")]
    [Holes(typeof(SequenceStoppedHoles))]
    SequenceStopped,

    // Why a sequence can't run.
    [Description(
        "Error: the sequence {Sequence} can't run: a sequence needs at least {Minimum} steps, and it has {Count}.")]
    [Holes(typeof(SequenceTooShortHoles))]
    SequenceTooShort,

    [Description(
        "Error: the sequence {Sequence} can't run: its step '{Step}' is no address in the Calls of the agent that " +
        "owns it.")]
    [Holes(typeof(SequenceStepHoles))]
    SequenceStepNotCallable,

    [Description("Error: the sequence {Sequence} can't run: it asks '{Step}' twice.")]
    [Holes(typeof(SequenceStepHoles))]
    SequenceStepTwice,

    [Description(
        "Error: the sequence {Sequence} can't run: its step '{Step}' is in the sequence {Other} too, and an address " +
        "belongs to one sequence only.")]
    [Holes(typeof(SequenceStepSharedHoles))]
    SequenceStepShared,

    [Description(
        "Error: the sequence {Sequence} can't run: its step '{Step}' is a lookup agent, and a step is an agent: an " +
        "identity's voice or one of its subagents.")]
    [Holes(typeof(SequenceStepHoles))]
    SequenceStepIsLookup,

    // An ask that can't go as the model wrote it.
    [Description(
        "Error: {Address} is step {Number} of the sequence {Sequence}, so it is never asked on its own. Ask " +
        "{First}, which runs the whole sequence in order.")]
    [Holes(typeof(SequenceLaterStepHoles))]
    SequenceLaterStep,

    [Description(
        "Error: {Address} starts the sequence {Sequence}, which takes neither a tool nor a stronger model. Ask it " +
        "without them.")]
    [Holes(typeof(SequenceTakesNoToolHoles))]
    SequenceTakesNoTool,
}
