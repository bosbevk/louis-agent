namespace LouisAgent.Api.Messages;

using System.ComponentModel;

// The message enums (docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by MessageText.Format (or by the structured logger). Members are added
// as code is written or moved; the refactor after F4 moves the existing messages here.

/// <summary>Exception texts for the API area: what is wrong and how to fix it.</summary>
public enum ApiErrorMessage
{
    [Description("depth must be between 0 and {Max}: a specialist is one level down, a minion two.")]
    SessionDepthInvalid,

    [Description("A sub-session can't load the '{Toolset}' toolset: only the agent the user talks to delegates to specialists.")]
    SubSessionCannotDelegate,

    [Description("A minion (depth {Depth}) names the one tool it gets: 'tool' is required.")]
    MinionNeedsTool,

    [Description("'tool' is for a minion (depth {MinionDepth}): a session at depth {Depth} loads toolsets.")]
    ToolOnlyForMinions,

    [Description("A minion needs its parent: 'parent' names the specialist's session and turn ({Example}).")]
    MinionNeedsParent,

    [Description("Unknown parent session '{Session}': a minion runs on its specialist's own instance.")]
    MinionParentUnknown,

    [Description("'{Tool}' is never handed to a minion.")]
    ToolNeverHandedDown,

    [Description("The parent session has no '{Tool}' tool: a minion gets one of its specialist's own tools.")]
    ParentLacksTool,

    [Description("The 'minions' toolset is a specialist's (depth 1): the agent the user talks to delegates to specialists, and a minion delegates to no one.")]
    MinionsOnlyForSpecialists,

    [Description("No toolset builds a '{Tool}' tool.")]
    NoToolsetForTool,
}
