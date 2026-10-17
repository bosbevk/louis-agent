namespace LouisAgent.Core.Skills;

using System.Text.RegularExpressions;

/// <summary>
/// Personalities a session can choose (F21), in the folder of the level that reads them (CL8, ADR 0017):
/// <c>agent/{name}.md</c> (level 0, a voice), <c>specialists/{name}.md</c> (level 1, a role),
/// <c>minions/{name}.md</c> (level 2, F30), under core's <c>Prompts/</c> and read only from the assembly (CL8
/// step 4). At level 0 a personality is a mode (CL9, ADR 0019): one file, its voice then its routing, in a folder per
/// product (<c>agent/puls/voice-af.md</c>), named <c>{product}/{mode}</c>; <see cref="DefaultVoice"/> is the mode of a
/// session that names none. A name is lowercase letters, digits and dashes, with one <c>/</c> between a product and its
/// mode, so it never reaches outside its folder.
/// </summary>
public static partial class Personalities
{
    /// <summary>Level 0's folder, under the skills folder: the voices of the agent the user talks to.</summary>
    public const string AgentFolder = "agent";

    /// <summary>Level 1's folder: one role per specialist, named by the parent (CL10: none is derived).</summary>
    public const string SpecialistsFolder = "specialists";

    /// <summary>Level 2's folder: one function per minion (F30).</summary>
    public const string MinionsFolder = "minions";

    private const int MaxNameLength = 40;

    /// <summary>The mode of a level-0 session that names none (CL9): the coding agent's (CLI, Rider, MCP).</summary>
    public const string DefaultVoice = "louis-agent/voice";

    /// <summary>
    /// The base a minion role sits on (F30): <c>{product}-minion</c> for <c>{product}-minion-{function}</c>
    /// (<c>puls-minion</c> for <c>puls-minion-web-price-lookup</c>), loaded before the role so every minion of a product
    /// is told what a minion is once. Null for a name that isn't a minion role's shape.
    /// </summary>
    public static string? MinionBaseOf(string role) => MinionRole().Match(role) is { Success: true } m ? m.Groups["base"].Value : null;

    /// <summary>Whether <paramref name="name"/> is a minion base (<c>puls-minion</c>), which is no role a specialist can name.</summary>
    public static bool IsMinionBase(string name) => MinionBase().IsMatch(name);

    /// <summary>The folder, under the skills folder, of <paramref name="level"/>'s personalities.</summary>
    public static string FolderOf(SkillLevel level) => level switch
    {
        SkillLevel.Agent => AgentFolder,
        SkillLevel.Specialist => SpecialistsFolder,
        SkillLevel.Minion => MinionsFolder,
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, null),
    };

    /// <summary>Whether <paramref name="name"/> can name a personality (and so can be turned into a path safely).</summary>
    public static bool IsValidName(string? name) => name is { Length: > 0 and <= MaxNameLength } && NamePattern().IsMatch(name);

    /// <summary>The skill file of a named personality at <paramref name="level"/>, relative to the skills folder.</summary>
    public static string FileName(string name, SkillLevel level) => $"{FolderOf(level)}/{name}.md";

    /// <summary>
    /// The named personalities at <paramref name="level"/>, all built in (CL8 step 4), sorted: at level 0 the modes,
    /// <c>agent/{product}/{mode}.md</c> (every product folder but the function skills'); at level 1 the roles; at level 2
    /// the minion roles <see cref="MinionRoles"/> lists (F30: a base, or a file the data file doesn't name, is no role).
    /// </summary>
    public static IReadOnlyList<string> Available(SkillLevel level) =>
        (level == SkillLevel.Agent ? Modes() : SkillFileSource.Files(FolderOf(level)).Where(f => f.EndsWith(".md", StringComparison.Ordinal))
            .Select(f => f[..^".md".Length]))
        .Where(IsValidName)
        .Where(n => level != SkillLevel.Minion || MinionRoles.ToolsOf(n) is not null)
        .Order(StringComparer.Ordinal)
        .ToList();

    /// <summary>
    /// A mode file's two parts: its voice (the first <c># </c> section) and its routing (from the second <c># </c> heading
    /// on, empty when there is none). A session below level 0 that names no role gets the default mode's voice only: its
    /// toolsets were chosen for it (F21).
    /// </summary>
    public static (string Voice, string Routing) ModeParts(string text)
    {
        var second = SecondHeading().Match(text);
        return second.Success ? (text[..second.Index].TrimEnd(), text[second.Index..].Trim()) : (text.Trim(), "");
    }

    /// <summary>
    /// The language a mode speaks (CL9 step 6): <c>{product}/voice-{xx}</c>, a two-letter code, is how the product talks in
    /// that language, and loads <see cref="SharedOf"/> after it; null for a whole mode (<c>louis-agent/voice</c>).
    /// </summary>
    public static string? LanguageOf(string mode) => LanguageMode().Match(mode) is { Success: true } m ? m.Groups["code"].Value : null;

    /// <summary>A language mode's shared part: its product's <c>voice</c> (Puls's routing), never a mode of its own.</summary>
    public static string SharedOf(string mode) => mode[..mode.IndexOf('/')] + "/voice";

    private static IEnumerable<string> Modes()
    {
        var files = SkillFileSource.EmbeddedFiles
            .Where(f => f.StartsWith(AgentFolder + "/", StringComparison.Ordinal) && f.EndsWith(".md", StringComparison.Ordinal)
                && !f.StartsWith(SkillFiles.FunctionsFolder + "/", StringComparison.Ordinal))
            .Select(f => f[(AgentFolder.Length + 1)..^".md".Length])
            .Where(n => n.Count(c => c == '/') == 1)
            .ToList();
        // A product with languages shares its voice.md between them; it isn't a mode of its own (puls/voice).
        var shared = files.Where(f => LanguageOf(f) is not null).Select(SharedOf).ToHashSet(StringComparer.Ordinal);
        return files.Where(f => !shared.Contains(f));
    }

    [GeneratedRegex("^[a-z0-9-]+/voice-(?<code>[a-z]{2})$")]
    private static partial Regex LanguageMode();

    [GeneratedRegex("^(?<base>[a-z0-9]+-minion)-[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex MinionRole();

    [GeneratedRegex("^[a-z0-9]+-minion$")]
    private static partial Regex MinionBase();

    // Not the first line: a mode file starts with its voice's heading.
    [GeneratedRegex(@"(?<=\n)# ", RegexOptions.None)]
    private static partial Regex SecondHeading();

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*(/[a-z0-9]+(-[a-z0-9]+)*)?$")]
    private static partial Regex NamePattern();
}
