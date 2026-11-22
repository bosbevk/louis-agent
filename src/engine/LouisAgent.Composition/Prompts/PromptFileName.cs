using LouisAgent.Choices;

namespace LouisAgent.Composition.Prompts;

/// <summary>
/// The files a folder is known by: an agent's or a subagent's voice, and a skill's own file, the shape Claude Code's
/// skills have.
/// </summary>
/// <remarks>An enum for the same reason as <see cref="PromptFolder"/> (ADR 0043).</remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0043-repeated-names-are-enums.md">ADR 0043: repeated names are enums</seealso>
public enum PromptFileName
{
    /// <summary>An agent's or a subagent's own prompt in its folder.</summary>
    [WireName("voice.md")] Voice,

    /// <summary>A skill's file in its folder, the skill named for the folder (ADR 0063).</summary>
    [WireName("SKILL.md")] Skill,
}
