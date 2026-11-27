using LouisAgent.Composition.Packs;

namespace LouisAgent.Core.Prompting.Headers.Agent.Library;

/// <summary>
/// The skills core ships that the model reads when it needs them (F97.1), such as <c>write-a-skill</c>: each a folder
/// in <c>skills/</c> whose header says <c>metadata.load: on-demand</c>. <c>UseSkill</c> reads one by its name; none
/// has a script, so none is run.
/// </summary>
/// <remarks>
/// Read from <see cref="PromptCatalog"/>, so every header was checked once at start-up. A toolset's own skill, which
/// says no load, is in the prompt already and is never one of these.
/// </remarks>
internal static class BuiltInSkills
{
    /// <summary>
    /// The text of the built-in on-demand skill of this name, its header left out; none for another name.
    /// </summary>
    public static string? TextOf(string name) =>
        PromptCatalog.Get(PackToolset.SkillFile(name)) is { Header.Load: SkillLoad.OnDemand } file
            ? file.Body.ReplaceLineEndings("\n")
            : null;

    /// <summary>Whether a built-in skill, of any kind, has this name, so an agent's skill can't take it.</summary>
    public static bool Has(string name) => PromptCatalog.Get(PackToolset.SkillFile(name)) is not null;
}
