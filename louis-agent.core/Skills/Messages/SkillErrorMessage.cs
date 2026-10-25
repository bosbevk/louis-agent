namespace LouisAgent.Core.Skills.Messages;

using System.ComponentModel;

// The message enums (docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by MessageText.Format (or by the structured logger). Members are added
// as code is written or moved; the refactor after F4 moves the existing messages here.

/// <summary>Exception texts for the skills and roles area: what is wrong and how to fix it.</summary>
public enum SkillErrorMessage
{
    [Description("'{Name}' isn't a role name: use lowercase letters, digits and dashes, and one / between a product and its mode.")]
    RoleNameInvalid,

    [Description("Unknown role '{Name}'. Available: {Available}.")]
    UnknownRole,

    [Description("A minion opens with a role (depth 2 names one). Available: {Available}.")]
    MinionNeedsRole,

    [Description("Prompts were added after the first one was read: register every identity and pack before the host builds.")]
    PromptsAddedAfterRead,

    [Description("ROLES_ENABLED names roles this instance doesn't have: {Names}. Available: {Available}.")]
    UnknownRolesEnabled,

    [Description("The default mode '{Mode}' has no file ({File}): a session that names no role would open with no mode.")]
    DefaultModeMissing,
}
