using System.Text.RegularExpressions;

namespace LouisAgent.Core.Skills.Roles;

/// <summary>
/// The roles a session can choose (F21): <c>agents/{name}.md</c> (level 0, a voice), a subagent inside its agent at
/// <c>agents/{identity}/subagents/{name}/voice.md</c> (level 1, ADR 0133), <c>minions/{name}.md</c> (level 2, F30),
/// at their project's root and read only from the assembly (CL8 step 4). At level 0 a role is a mode (CL9, ADR
/// 0019): one file, its voice then its routing, in a folder per product (<c>agents/acme/voice-en.md</c>), named
/// <c>{product}/{mode}</c>; an identity's default is the mode of a session that names none. At level 1 a subagent is
/// named by its address, <c>{identity}.{name}</c>. A name is lowercase letters, digits and dashes, with one <c>/</c>
/// between a product and its mode or one <c>.</c> between an identity and its subagent, so it never reaches outside its
/// folder.
/// </summary>
public static partial class RoleCatalog
{
    /// <summary>Level 0's folder, under the skills folder: the voices of the agent the user talks to.</summary>
    public const string AgentFolder = "agents";

    /// <summary>
    /// Where an agent keeps its subagents, level 1 (ADR 0133): <c>agents/{identity}/subagents/{name}/</c>, a folder
    /// each.
    /// </summary>
    public const string SubagentsFolder = "subagents";

    /// <summary>A subagent's own prompt in its folder, as an agent's is in its.</summary>
    public const string VoiceFile = "voice.md";

    /// <summary>Level 2's folder: one function per minion (F30).</summary>
    public const string MinionsFolder = "minions";

    private const int MaxNameLength = 40;

    /// <summary>
    /// The base a minion role sits on (F30): <c>{product}-minion</c> for <c>{product}-minion-{function}</c>
    /// (<c>acme-minion</c> for <c>acme-minion-web-lookup</c>), loaded before the role so every minion of a product
    /// is told what a minion is once. Null for a name that isn't a minion role's shape.
    /// </summary>
    public static string? MinionBaseOf(string role) =>
        MinionRole().Match(role) is { Success: true } m ? m.Groups["base"].Value : null;

    /// <summary>
    /// Whether <paramref name="name"/> is a minion base (<c>acme-minion</c>), which is no role a specialist can name.
    /// </summary>
    public static bool IsMinionBase(string name) => MinionBase().IsMatch(name);

    /// <summary>
    /// The folder of a level whose roles sit in one place: the agent's modes, and the minions. A subagent sits inside
    /// its own agent, so it has none.
    /// </summary>
    private static string FolderOf(RoleLevel level) => level switch
    {
        RoleLevel.Agent => AgentFolder,
        RoleLevel.Minion => MinionsFolder,
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, null),
    };

    /// <summary>Whether <paramref name="name"/> can name a role (and so can be turned into a path safely).</summary>
    public static bool IsValidName(string? name) =>
        name is { Length: > 0 and <= MaxNameLength } && NamePattern().IsMatch(name);

    /// <summary>
    /// The skill file of a named role at <paramref name="level"/>, relative to the skills folder: a subagent's
    /// address, <c>acme.cook</c>, is the file <c>agents/acme/subagents/cook/voice.md</c>.
    /// </summary>
    public static string FileName(string name, RoleLevel level) => level == RoleLevel.Subagent
        ? SubagentFile(name)
        : $"{FolderOf(level)}/{name}.md";

    /// <summary>
    /// The named roles at <paramref name="level"/>, all built in (CL8 step 4), sorted: at level 0 the modes,
    /// <c>agents/{product}/{mode}.md</c> (every product folder but the function skills'); at level 1 the subagents by
    /// their addresses; at level 2 the minion roles, never a base (F30: <c>acme-minion</c> frames every minion, it is
    /// no role itself).
    /// </summary>
    public static IReadOnlyList<string> Available(RoleLevel level) =>
        (level switch
        {
            RoleLevel.Agent => Modes(),
            RoleLevel.Subagent => Subagents(),
            _ => SkillFileSource.Files(FolderOf(level)).Where(f => f.EndsWith(".md", StringComparison.Ordinal))
                .Select(f => f[..^".md".Length])
        })
        .Where(IsValidName)
        .Where(n => level != RoleLevel.Minion || !IsMinionBase(n))
        .Order(StringComparer.Ordinal)
        .ToList();

    /// <summary>
    /// The level that has a role by this name, or none (F127.3.1): a mode is the agent's, an address an agent's
    /// subagent's, one in <c>minions/</c> a minion's. A name is at one level only.
    /// </summary>
    public static RoleLevel? LevelOf(string? role)
    {
        if (role is null) return null;
        foreach (var level in Enum.GetValues<RoleLevel>())
            if (Available(level).Contains(role))
                return level;
        return null;
    }

    /// <summary>
    /// A mode file's two parts: its voice (the first <c># </c> section) and its routing (from the second <c># </c>
    /// heading on, empty when there is none). A session below level 0 that names no role gets the default mode's voice
    /// only: its toolsets were chosen for it (F21).
    /// </summary>
    public static (string Voice, string Routing) ModeParts(string text)
    {
        var second = SecondHeading().Match(text);
        return second.Success ? (text[..second.Index].TrimEnd(), text[second.Index..].Trim()) : (text.Trim(), "");
    }

    /// <summary>
    /// The language a mode speaks (CL9 step 6): <c>{product}/voice-{xx}</c>, a two-letter code, is how the product
    /// talks in that language, and loads <see cref="SharedOf"/> after it; null for a whole mode
    /// (<c>louis-agent/voice</c>).
    /// </summary>
    public static string? LanguageOf(string mode) =>
        LanguageMode().Match(mode) is { Success: true } m ? m.Groups["code"].Value : null;

    /// <summary>
    /// A language mode's shared part: its product's <c>voice</c> (its routing), never a mode of its own.
    /// </summary>
    public static string SharedOf(string mode) => mode[..mode.IndexOf('/')] + "/voice";

    /// <summary>
    /// The modes in <c>agents/</c>, one per <c>{product}/{mode}.md</c>: not the function skills, not a product's shared
    /// <c>voice.md</c> when it has language modes.
    /// </summary>
    private static IEnumerable<string> Modes()
    {
        var files = SkillFileSource.EmbeddedFiles
            .Where(f => f.StartsWith(AgentFolder + "/", StringComparison.Ordinal) &&
                        f.EndsWith(".md", StringComparison.Ordinal)
                        && !f.StartsWith(SkillFiles.FunctionsFolder + "/", StringComparison.Ordinal))
            .Select(f => f[(AgentFolder.Length + 1)..^".md".Length])
            .Where(n => n.Count(c => c == '/') == 1)
            .ToList();
        // A product with languages shares its voice.md between them; it isn't a mode of its own ({product}/voice).
        var shared = files.Where(f => LanguageOf(f) is not null).Select(SharedOf).ToHashSet(StringComparer.Ordinal);
        return files.Where(f => !shared.Contains(f));
    }

    /// <summary>
    /// Every agent's subagents by their addresses: <c>agents/acme/subagents/cook/voice.md</c> is <c>acme.cook</c>.
    /// </summary>
    private static IEnumerable<string> Subagents() =>
        SkillFileSource.EmbeddedFiles
            .Select(file => file.Split('/'))
            .Where(parts => parts is [AgentFolder, _, SubagentsFolder, _, VoiceFile])
            .Select(parts => $"{parts[1]}.{parts[3]}");

    /// <summary>
    /// A subagent's file from its address, inside its agent; a name without its identity points at no file, so it is
    /// refused as unknown.
    /// </summary>
    private static string SubagentFile(string address) => address.Split('.') is [var identity, var subagent]
        ? $"{AgentFolder}/{identity}/{SubagentsFolder}/{subagent}/{VoiceFile}"
        : $"{AgentFolder}/{SubagentsFolder}/{address}/{VoiceFile}";

    /// <summary>A language mode: matches <c>acme/voice-af</c> (code <c>af</c>), not <c>acme/voice</c>.</summary>
    [GeneratedRegex("^[a-z0-9-]+/voice-(?<code>[a-z]{2})$")]
    private static partial Regex LanguageMode();

    /// <summary>A minion role on its base: matches <c>acme-minion-web-lookup</c>, not <c>acme-minion</c>.</summary>
    [GeneratedRegex("^(?<base>[a-z0-9]+-minion)-[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex MinionRole();

    /// <summary>A minions' base: matches <c>acme-minion</c>, not <c>acme-minion-web-lookup</c>.</summary>
    [GeneratedRegex("^[a-z0-9]+-minion$")]
    private static partial Regex MinionBase();

    /// <summary>
    /// A heading after the first line, since a mode file starts with its voice's: matches the <c># </c> of
    /// <c>"# Voice\n# Routing"</c> at its second line, not at its first.
    /// </summary>
    [GeneratedRegex(@"(?<=\n)# ", RegexOptions.None)]
    private static partial Regex SecondHeading();

    /// <summary>
    /// A role's name: matches <c>acme.cook</c>, <c>acme-minion-lookup</c> and <c>acme/voice-en</c>, not
    /// <c>../acme</c> or <c>a.b.c</c>.
    /// </summary>
    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*([/.][a-z0-9]+(-[a-z0-9]+)*)?$")]
    private static partial Regex NamePattern();
}
