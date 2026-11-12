using LouisAgent.Composition.Messages;
using System.ComponentModel;

namespace LouisAgent.Api.Messages;
// The message enums (src/docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by MessageText.Format (or by the structured logger). Members are
// added as code is written or moved; the refactor after F4 moves the existing messages here.

/// <summary>Exception texts for the API area: what is wrong and how to fix it.</summary>
public enum ApiErrorMessage
{
    [Description(
        "depth must be between 0 and {Max} (AGENT_MAX_DEPTH): each agent asked is one level below the one that asked.")]
    SessionDepthInvalid,

    [Description("A minion can't load the '{Toolset}' toolset: a minion asks no one, it does its one task.")]
    MinionCannotAsk,

    [Description("A minion ('{Role}') names the one tool it gets: 'tool' is required.")]
    MinionNeedsTool,

    [Description("'tool' is for a minion's role: '{Role}' loads toolsets.")]
    ToolOnlyForMinions,

    [Description(
        "'{Agent}' is already in this chain of asks ({Chain}): an agent is never asked twice in one chain, so the " +
        "asks can't go round in a loop.")]
    AgentAlreadyInChain,

    [Description("A minion needs its parent: 'parent' names the specialist's session and turn ({Example}).")]
    MinionNeedsParent,

    [Description("Unknown parent session '{Session}': a minion runs on its specialist's own instance.")]
    MinionParentUnknown,

    [Description("'{Tool}' is never handed to a minion.")]
    ToolNeverHandedDown,

    [Description("The parent session has no '{Tool}' tool: a minion gets one of its specialist's own tools.")]
    ParentLacksTool,

    [Description("No toolset builds a '{Tool}' tool.")]
    NoToolsetForTool,

    [Description("This instance runs no identity '{Name}'; it runs: {Available}.")]
    UnknownIdentity,
}
