using System.ComponentModel;
using LouisAgent.Composition.Messages;
using LouisAgent.Evals.Messages.Holes.Grading;

namespace LouisAgent.Evals.Messages;

/// <summary>
/// What code saw when it told a fact, one plain line a mark carries whether the fact was met or not (ADR 0009: a text
/// is a member of a message enum, its <c>[Description]</c> a template with named holes).
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
public enum FactMessage
{
    [Description("The turn ended without an error.")]
    EndedWell,

    [Description("The turn ended with an error: {Error}")]
    [Holes(typeof(EndedWithErrorHoles))]
    EndedWithError,

    [Description("Tools called: {Tools}.")]
    [Holes(typeof(ToolsCalledHoles))]
    ToolsCalled,

    [Description("No tool was called.")]
    NoToolCalled,

    [Description("Took {Seconds} s.")]
    [Holes(typeof(TookHoles))]
    Took,

    [Description("The answer doesn't hold '{Text}'.")]
    [Holes(typeof(AnswerTextHoles))]
    AnswerDoesntHold,

    [Description("The answer holds '{Text}'.")]
    [Holes(typeof(AnswerTextHoles))]
    AnswerHolds,

    [Description("Agents asked: {Addresses}.")]
    [Holes(typeof(AgentsAskedHoles))]
    AgentsAsked,

    [Description("No agent was asked.")]
    NoAgentAsked,

    [Description("Asked {Address} in this turn and in an earlier one.")]
    [Holes(typeof(AgentAddressHoles))]
    AskedNowAndBefore,

    [Description("{Address} wasn't asked in this turn.")]
    [Holes(typeof(AgentAddressHoles))]
    NotAskedNow,

    [Description("Asked {Address} in this turn, but in no earlier one.")]
    [Holes(typeof(AgentAddressHoles))]
    NotAskedBefore
}
