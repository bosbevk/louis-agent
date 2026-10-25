namespace LouisAgent.Core.Agent.Messages;

using System.ComponentModel;

// The message enums (docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by MessageText.Format (or by the structured logger). Members are added
// as code is written or moved; the refactor after F4 moves the existing messages here.

/// <summary>Exception texts for the agent area: what is wrong and how to fix it.</summary>
public enum AgentErrorMessage
{
    [Description("Two tools are named '{Name}'; tool names must be unique across all toolsets.")]
    DuplicateToolName,

    [Description("Unknown toolset(s): {Names}. Valid names: {Valid}.")]
    UnknownToolsets,

    [Description("Toolset '{Name}' needs {Settings} to be set on this instance.")]
    ToolsetSettingsMissing,

    [Description("'{Name}' is registered twice as a {Kind}: a pack, toolset, role or prompt path has one home.")]
    CatalogNameTwice,

    [Description("'{Owner}' names the pack '{Name}', which isn't registered. Registered: {Registered}.")]
    CatalogPackUnknown,

    [Description("Packs require each other in a cycle: {Cycle}.")]
    CatalogRequiresCycle,

    [Description("Tool '{Tool}' is in two toolsets, '{First}' and '{Second}'; tool names are unique across all toolsets.")]
    CatalogToolNameTwice,

    [Description("Tool '{Tool}' in toolset '{Toolset}' takes the engine's own tool name.")]
    CatalogToolNameIsTheEngines,
}
