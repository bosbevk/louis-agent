
using System.ComponentModel;

namespace LouisAgent.Core.Config.Messages;
// The message enums (docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by MessageText.Format (or by the structured logger). Members are added
// as code is written or moved; the refactor after F4 moves the existing messages here.

/// <summary>Log lines for the settings, written to the console and the run file.</summary>
public enum ConfigLogMessage
{
    [Description(
        "{Old} is the setting's name before CL14; it's read until it's removed. Rename it {New} in config/.env.")]
    RolesSettingRenamed,

    [Description(
        "{Old} is ignored: {New} is set, and wins. In config/.env, rename {Old} to {New} (or remove it if {New} is there already).")]
    RolesSettingIgnored,

    [Description("WORKSPACE_ROOT not set, using repository root: {Path}")]
    WorkspaceRootFromRepository,

    [Description("WORKSPACE_ROOT not set, using current directory: {Path}")]
    WorkspaceRootFromCurrentDirectory,
}
