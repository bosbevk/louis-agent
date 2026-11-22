using LouisAgent.Composition.Messages;
using System.ComponentModel;
using LouisAgent.Core.Skills.Messages.Holes.Roles;

namespace LouisAgent.Core.Skills.Messages;
// The message enums (src/docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by MessageText.Format (or by the structured logger). Members are
// added as code is written or moved; the refactor after F4 moves the existing messages here.

/// <summary>Exception texts for the skills and roles area: what is wrong and how to fix it.</summary>
public enum SkillErrorMessage
{
    [Description(
        "'{Name}' isn't a role name: use lowercase letters, digits and dashes, with one / between a product and its " +
        "mode or one . between an identity and its subagent.")]
    [Holes(typeof(RoleNameInvalidHoles))]
    RoleNameInvalid,

    [Description("Unknown role '{Name}'. Available: {Available}.")]
    [Holes(typeof(UnknownRoleHoles))]
    UnknownRole,

    [Description("A lookup agent opens with a role (depth 2 names one). Available: {Available}.")]
    [Holes(typeof(LookupNeedsRoleHoles))]
    LookupNeedsRole,

    [Description(
        "Prompts were added after the first one was read: register every identity and pack before the host builds.")]
    PromptsAddedAfterRead,

    [Description("ROLES_ENABLED names roles this instance doesn't have: {Names}. Available: {Available}.")]
    [Holes(typeof(UnknownRolesEnabledHoles))]
    UnknownRolesEnabled,

    [Description(
        "The default mode '{Mode}' has no file ({File}): a session that names no role would open with no mode.")]
    [Holes(typeof(DefaultModeMissingHoles))]
    DefaultModeMissing,
}
