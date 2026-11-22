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

    [Description(
        "A lookup agent can't load the '{Toolset}' toolset: a lookup agent asks no one, it does its one task.")]
    LookupCannotAsk,

    [Description("A lookup agent ('{Role}') names the one tool it gets: 'tool' is required.")]
    LookupNeedsTool,

    [Description("'tool' is for a lookup agent: '{Role}' loads toolsets.")]
    ToolOnlyForLookups,

    [Description(
        "'{Agent}' is already in this chain of asks ({Chain}): an agent is never asked twice in one chain, so the " +
        "asks can't go round in a loop.")]
    AgentAlreadyInChain,

    [Description("A lookup agent needs its parent: 'parent' names the subagent's session and turn ({Example}).")]
    LookupNeedsParent,

    [Description("Unknown parent session '{Session}': a lookup agent runs on its subagent's own instance.")]
    LookupParentUnknown,

    [Description("'{Tool}' is never handed to a lookup agent.")]
    ToolNeverHandedDown,

    [Description("The parent session has no '{Tool}' tool: a lookup agent gets one of its subagent's own tools.")]
    ParentLacksTool,

    [Description("No toolset builds a '{Tool}' tool.")]
    NoToolsetForTool,

    [Description("This instance runs no identity '{Name}'; it runs: {Available}.")]
    UnknownIdentity,
}
