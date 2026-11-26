using LouisAgent.Composition.Messages;
using System.ComponentModel;
using LouisAgent.Core.Skills.Messages.Holes.Library;
using LouisAgent.Core.Skills.Messages.Holes.Prompts;

namespace LouisAgent.Core.Skills.Messages;

/// <summary>
/// Log lines for the skills, written to the console and the run file. Each member is named for what happened, and its
/// [Description] is the text, with named holes filled by the structured logger from the record its [Holes] names
/// (src/docs/CLAUDE.md, *Messages and logging*).
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0119-declare-name-then-pass.md">ADR 0119: named types, declared, then passed</seealso>
public enum SkillLogMessage
{
    [Description("The skill in {Folder} is left out: {Reason}")]
    [Holes(typeof(SkillLeftOutHoles))]
    SkillLeftOut,

    [Description(
        "{File} is ignored: built-in prompts are read from the assembly; change the one in " +
        "src/engine/LouisAgent.Core/Prompts and rebuild")]
    [Holes(typeof(BuiltInCopyIgnoredHoles))]
    BuiltInCopyIgnored,
}
