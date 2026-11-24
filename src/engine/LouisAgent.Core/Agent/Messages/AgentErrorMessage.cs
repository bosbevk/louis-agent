using LouisAgent.Composition.Messages;
using LouisAgent.Core.Agent.Messages.Holes.Catalog;
using LouisAgent.Core.Agent.Messages.Holes.Replay;
using System.ComponentModel;
using LouisAgent.Core.Agent.Messages.Holes.Toolsets;

namespace LouisAgent.Core.Agent.Messages;
// The message enums (src/docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by MessageText.Format (or by the structured logger). Members are
// added as code is written or moved; the refactor after F4 moves the existing messages here.

/// <summary>Exception texts for the agent area: what is wrong and how to fix it.</summary>
public enum AgentErrorMessage
{
    [Description("Two tools are named '{Name}'; tool names must be unique across all toolsets.")]
    [Holes(typeof(DuplicateToolNameHoles))]
    DuplicateToolName,

    [Description("Unknown toolset(s): {Names}. Valid names: {Valid}.")]
    [Holes(typeof(UnknownToolsetsHoles))]
    UnknownToolsets,

    [Description("Toolset '{Name}' needs {Settings} to be set on this instance.")]
    [Holes(typeof(ToolsetSettingsMissingHoles))]
    ToolsetSettingsMissing,

    [Description("'{Name}' is registered twice as a {Kind}: a pack, toolset, feed, role or prompt path has one home.")]
    [Holes(typeof(CatalogNameTwiceHoles))]
    CatalogNameTwice,

    [Description("'{Owner}' names the pack '{Name}', which isn't registered. Registered: {Registered}.")]
    [Holes(typeof(CatalogPackUnknownHoles))]
    CatalogPackUnknown,

    [Description("Packs require each other in a cycle: {Cycle}.")]
    [Holes(typeof(CatalogRequiresCycleHoles))]
    CatalogRequiresCycle,

    [Description(
        "Tool '{Tool}' is in two toolsets, '{First}' and '{Second}'; tool names are unique across all toolsets.")]
    [Holes(typeof(CatalogToolNameTwiceHoles))]
    CatalogToolNameTwice,

    [Description("Tool '{Tool}' in toolset '{Toolset}' takes the engine's own tool name.")]
    [Holes(typeof(CatalogToolNameIsTheEnginesHoles))]
    CatalogToolNameIsTheEngines,

    // A tool's answers replayed (FX235). A miss never runs the tool.
    [Description(
        "No recorded answer for the tool '{Tool}' with these arguments in trial '{Trial}'. The tool was not run.")]
    [Holes(typeof(ToolReplayMissHoles))]
    ToolReplayMiss,
}
