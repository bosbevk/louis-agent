using LouisAgent.Composition.Messages;
using System.ComponentModel;
using LouisAgent.Api.Messages.Holes.Lookups;
using LouisAgent.Api.Messages.Holes.Opening;
using LouisAgent.Api.Messages.Holes.Turns;

namespace LouisAgent.Api.Messages;
// The message enums (src/docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by MessageText.Format (or by the structured logger). Members are
// added as code is written or moved; the refactor after F4 moves the existing messages here.

/// <summary>Exception texts for the API area: what is wrong and how to fix it.</summary>
public enum ApiErrorMessage
{
    [Description(
        "depth must be between 0 and {Max} (AGENT_MAX_DEPTH): each agent asked is one level below the one that asked.")]
    [Holes(typeof(SessionDepthInvalidHoles))]
    SessionDepthInvalid,

    [Description(
        "A lookup agent can't load the '{Toolset}' toolset: a lookup agent asks no one, it does its one task.")]
    [Holes(typeof(LookupCannotAskHoles))]
    LookupCannotAsk,

    [Description("A lookup agent ('{Role}') names the one tool it gets: 'tool' is required.")]
    [Holes(typeof(LookupNeedsToolHoles))]
    LookupNeedsTool,

    [Description("'tool' is for a lookup agent: '{Role}' loads toolsets.")]
    [Holes(typeof(ToolOnlyForLookupsHoles))]
    ToolOnlyForLookups,

    [Description(
        "'{Agent}' is already in this chain of asks ({Chain}): an agent is never asked twice in one chain, so the " +
        "asks can't go round in a loop.")]
    [Holes(typeof(AgentAlreadyInChainHoles))]
    AgentAlreadyInChain,

    [Description("A lookup agent needs its parent: 'parent' names the subagent's session and turn ({Example}).")]
    [Holes(typeof(LookupNeedsParentHoles))]
    LookupNeedsParent,

    [Description("Unknown parent session '{Session}': a lookup agent runs on its subagent's own instance.")]
    [Holes(typeof(LookupParentUnknownHoles))]
    LookupParentUnknown,

    [Description("'{Tool}' is never handed to a lookup agent.")]
    [Holes(typeof(ToolNeverHandedDownHoles))]
    ToolNeverHandedDown,

    [Description("The parent session has no '{Tool}' tool: a lookup agent gets one of its subagent's own tools.")]
    [Holes(typeof(ParentLacksToolHoles))]
    ParentLacksTool,

    [Description("No toolset builds a '{Tool}' tool.")]
    [Holes(typeof(NoToolsetForToolHoles))]
    NoToolsetForTool,

    [Description("This instance runs no identity '{Name}'; it runs: {Available}.")]
    [Holes(typeof(UnknownIdentityHoles))]
    UnknownIdentity,

    [Description("SESSION_STORE is 'memory' or 'mongo': where the chats are kept.")]
    UnknownSessionStore,

    [Description("SESSION_STORE is 'mongo', so SESSION_STORE_URL must name the MongoDB the chats are kept in.")]
    SessionStoreUrlMissing,

    [Description(
        "SESSION_RETENTION is how long an idle chat is kept, a .NET duration above zero, such as 30.00:00:00 for " +
        "30 days.")]
    SessionRetentionInvalid,

    [Description("Turn {Number} of {Session} is already kept; a turn is written once.")]
    [Holes(typeof(KeptTurnHoles))]
    TurnAlreadyKept,
}
