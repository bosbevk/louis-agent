using LouisAgent.Composition.Messages;
using System.ComponentModel;
using LouisAgent.Core.Skills.Messages.Holes.Folders;
using LouisAgent.Core.Skills.Messages.Holes.Parameters;
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
    [Description(
        "'{Folder}' has no SKILL.md: a skill is a folder holding its SKILL.md, its parameters.schema.json and its " +
        "script.")]
    [Holes(typeof(SkillFileMissingHoles))]
    SkillFileMissing,
    [Description("{Skill}'s SKILL.md won't read: {Reason}")]
    [Holes(typeof(SkillHeaderUnreadHoles))]
    SkillHeaderUnread,
    [Description(
        "{Skill}'s SKILL.md names no script: add metadata.script, its path from the skill's folder, such as " +
        "scripts/run.py.")]
    [Holes(typeof(ScriptNotNamedHoles))]
    ScriptNotNamed,
    [Description("{Skill}'s script '{Script}' is outside its folder: name a file inside it, such as scripts/run.py.")]
    [Holes(typeof(ScriptOutsideFolderHoles))]
    ScriptOutsideFolder,
    [Description("{Skill}'s script '{Script}' isn't there: write it, or name the file that is.")]
    [Holes(typeof(ScriptMissingHoles))]
    ScriptMissing,
    [Description("{Skill}'s script is a '{Extension}' file, which isn't run here: use {Known}.")]
    [Holes(typeof(ScriptLanguageUnknownHoles))]
    ScriptLanguageUnknown,
    [Description(
        "{Skill} has no parameters.schema.json: write a JSON Schema of type object, with each parameter it takes " +
        "under properties, its type and description, and the ones a call must give under required.")]
    [Holes(typeof(SchemaMissingHoles))]
    SchemaMissing,
    [Description(
        "{Skill}'s parameters.schema.json isn't a JSON Schema object: give it type object, its parameters under " +
        "properties, and the ones a call must give under required.")]
    [Holes(typeof(SchemaNotAnObjectHoles))]
    SchemaNotAnObject,
    [Description("{Skill}'s parameter '{Parameter}' has no type that's taken here: give it one of {Known}.")]
    [Holes(typeof(ParameterTypeUnknownHoles))]
    ParameterTypeUnknown,
}
