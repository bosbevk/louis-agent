
using System.ComponentModel;

namespace LouisAgent.Core.Skills.Messages;
// The message enums (docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by MessageText.Format (or by the structured logger). Members are added
// as code is written or moved; the refactor after F4 moves the existing messages here.

/// <summary>Log lines for the skills, written to the console and the run file.</summary>
public enum SkillLogMessage
{
    [Description(
        "{File} is ignored: built-in prompts are read from the assembly; change the one in louis-agent.core/Prompts and rebuild")]
    BuiltInCopyIgnored,
}
