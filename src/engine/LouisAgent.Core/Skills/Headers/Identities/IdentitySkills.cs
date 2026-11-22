using LouisAgent.Choices;
using LouisAgent.Composition.Prompts;

namespace LouisAgent.Core.Skills.Headers.Identities;

/// <summary>
/// The skills an identity keeps beside its voice (F129), each a folder in <c>agents/{identity}/skills/</c> holding its
/// <c>SKILL.md</c> (CL68.1): the loader puts the always ones in the prompt and lists the on-demand ones, and
/// <c>ReadSkill</c> finds one by its name.
/// </summary>
/// <remarks>
/// Progressive disclosure, as Anthropic's Agent Skills load theirs: what every turn needs is in the prompt, and the
/// rest is a name and a line until the model asks for it. Read from <see cref="PromptCatalog"/>, so every header was
/// checked when the host started.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0114-an-identitys-skills-load-always-or-on-demand.md">ADR 0114: an identity's skills load always or on demand</seealso>
public static class IdentitySkills
{
    /// <summary>The skills of the identity a mode (<c>{identity}/{mode}</c>) belongs to, by name.</summary>
    public static IReadOnlyList<IdentitySkill> OfMode(string mode) =>
        mode.IndexOf('/') is var slash and > 0 ? Of(mode[..slash]) : [];

    /// <summary>
    /// An identity's skills, by name: the <c>SKILL.md</c> of each folder directly in its skills folder.
    /// </summary>
    public static IReadOnlyList<IdentitySkill> Of(string identity)
    {
        string folder = SkillFiles.SkillsOf(identity);
        return PromptCatalog.All
            .Where(p => p.Path.StartsWith(folder, StringComparison.Ordinal) && IsASkillsFile(p.Path[folder.Length..]))
            .Select(p => IdentitySkill.Of(identity, p))
            .ToList();
    }

    /// <summary>Whether a path in a skills folder is one skill's file: <c>{name}/SKILL.md</c>, a folder down.</summary>
    private static bool IsASkillsFile(string inFolder) =>
        inFolder.Split('/') is [_, var file] && file == PromptFileName.Skill.Wire();
}
