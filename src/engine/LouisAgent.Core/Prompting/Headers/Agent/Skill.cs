using LouisAgent.Core.Prompting.Languages;

namespace LouisAgent.Core.Prompting.Headers.Agent;

/// <summary>
/// One skill the agent can use (F97.1), read from its folder: what its <c>SKILL.md</c> says it is and how to use it,
/// the script it runs and in which language, what it takes, and whether it is a draft or approved.
/// </summary>
/// <param name="Name">Its folder's name, which its header gives too.</param>
/// <param name="Description">The header's line saying what it does and when to use it.</param>
/// <param name="Instructions">The body of its <c>SKILL.md</c>, read when the model asks for it.</param>
/// <param name="Folder">Its folder.</param>
/// <param name="Script">The script it runs, a file inside its folder.</param>
/// <param name="Language">The script's language, by its extension.</param>
/// <param name="Parameters">What a call must give it.</param>
/// <param name="State">A draft, or approved.</param>
/// <seealso href="https://agentskills.io/specification">Agent Skills: the specification</seealso>
public sealed record Skill(
    string Name,
    string Description,
    string Instructions,
    string Folder,
    string Script,
    ScriptLanguage Language,
    SkillParameters Parameters,
    SkillState State);
