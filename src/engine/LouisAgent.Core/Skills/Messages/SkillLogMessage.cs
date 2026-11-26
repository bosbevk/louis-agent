
using LouisAgent.Composition.Messages;
using System.ComponentModel;
using LouisAgent.Core.Skills.Messages.Holes.Library;

namespace LouisAgent.Core.Skills.Messages;
// The message enums (src/docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by MessageText.Format (or by the structured logger). Members are
// added as code is written or moved; the refactor after F4 moves the existing messages here.

/// <summary>Log lines for the skills, written to the console and the run file.</summary>
public enum SkillLogMessage
{
    [Description("The skill in {Folder} is left out: {Reason}")]
    [Holes(typeof(SkillLeftOutHoles))]
    SkillLeftOut,
    [Description(
        "{File} is ignored: built-in prompts are read from the assembly; change the one in " +
        "src/engine/LouisAgent.Core/Prompts and rebuild")]
    BuiltInCopyIgnored,
}
