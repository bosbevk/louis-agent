using System.Text.RegularExpressions;
using LouisAgent.Choices;
using LouisAgent.Composition.Prompts;

namespace LouisAgent.Core.Skills.Roles;

/// <summary>
/// The roles a session can choose (F21): <c>agents/{name}.md</c> (level 0, a voice), a subagent inside its agent at
/// <c>agents/{identity}/subagents/{name}/voice.md</c> (level 1, ADR 0133), a lookup agent at
/// <c>agents/{identity}/lookups/{name}/voice.md</c> (level 2, F30, ADR 0021),
/// at their project's root and read only from the assembly (CL8 step 4). At level 0 a role is a mode (CL9, ADR
/// 0019): one file, its voice then its routing, in a folder per product (<c>agents/acme/voice-en.md</c>), named
/// <c>{product}/{mode}</c>; an identity's default is the mode of a session that names none. At level 1 a subagent is
/// named by its address, <c>{identity}.{name}</c>. A name is lowercase letters, digits and dashes, with one <c>/</c>
/// between a product and its mode or one <c>.</c> between an identity and its subagent, so it never reaches outside its
/// folder.
/// </summary>
public static partial class RoleCatalog
{
    /// <summary>The agents' folder, written once from the shared names (ADR 0043).</summary>
    private static readonly string Agents = PromptFolder.Agents.Wire();

    /// <summary>Where an agent keeps its subagents, a folder each (ADR 0133).</summary>
    private static readonly string SubagentsFolder = PromptFolder.Subagents.Wire();

    /// <summary>
    /// Where an agent keeps its lookup agents, a folder each, apart from its subagents (CL68.3, Louis's pick B: "will
    /// read better thinking of big identities").
    /// </summary>
    private static readonly string LookupsFolder = PromptFolder.Lookups.Wire();

    /// <summary>An agent's, a subagent's or a lookup agent's own prompt in its folder.</summary>
    private static readonly string Voice = PromptFileName.Voice.Wire();

    /// <summary>What every lookup agent of an identity loads first, at its agent's root.</summary>
    private static readonly string LookupBase = PromptFileName.Lookup.Wire();

    private const int MaxNameLength = 40;

    /// <summary>
    /// The base every lookup agent of an identity loads first (F30, CL68.3), so each is told once what a lookup agent
    /// is: <c>acme.lookup</c>'s is <c>agents/acme/lookup.md</c>. None for a name that isn't an address.
    /// </summary>
    public static string? LookupBaseOf(string role) => role.Split('.') is [var identity, _]
        ? $"{Agents}/{identity}/{LookupBase}"
        : null;

    /// <summary>Whether <paramref name="name"/> can name a role (and so can be turned into a path safely).</summary>
    public static bool IsValidName(string? name) =>
        name is { Length: > 0 and <= MaxNameLength } && NamePattern().IsMatch(name);

    /// <summary>
    /// The skill file of a named role at <paramref name="level"/>, relative to the skills folder: a subagent's
    /// address, <c>acme.cook</c>, is the file <c>agents/acme/subagents/cook/voice.md</c>, a lookup agent's,
    /// <c>acme.lookup</c>, <c>agents/acme/lookups/lookup/voice.md</c>.
    /// </summary>
    public static string FileName(string name, RoleLevel level) => level switch
    {
        RoleLevel.Subagent => HelperFile(name, SubagentsFolder),
        RoleLevel.Lookup => HelperFile(name, LookupsFolder),
        _ => $"{Agents}/{name}.md",
    };

    /// <summary>
    /// The named roles at <paramref name="level"/>, all built in (CL8 step 4), sorted: at level 0 the modes,
    /// <c>agents/{product}/{mode}.md</c> (every product folder but the function skills'); at level 1 the subagents by
    /// their addresses; at level 2 the lookup agents by their addresses, never a base (F30: the base frames every
    /// lookup agent, it is no role itself).
    /// </summary>
    public static IReadOnlyList<string> Available(RoleLevel level) =>
        (level switch
        {
            RoleLevel.Agent => Modes(),
            RoleLevel.Subagent => Helpers(SubagentsFolder),
            _ => Helpers(LookupsFolder)
        })
        .Where(IsValidName)
        .Order(StringComparer.Ordinal)
        .ToList();

    /// <summary>
    /// The level that has a role by this name, or none (F127.3.1): a mode is the agent's, an address an agent's
    /// subagent's or lookup agent's. A name is at one level only.
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
    /// <c>voice.md</c> when it has language modes, and not its lookup agents' base, <c>lookup.md</c>.
    /// </summary>
    private static IEnumerable<string> Modes()
    {
        var files = SkillFileSource.EmbeddedFiles
            .Where(f => f.StartsWith(Agents + "/", StringComparison.Ordinal) &&
                        f.EndsWith(".md", StringComparison.Ordinal)
                        && !f.StartsWith(SkillFiles.FunctionsFolder + "/", StringComparison.Ordinal)
                        && Path.GetFileName(f) != LookupBase)
            .Select(f => f[(Agents.Length + 1)..^".md".Length])
            .Where(n => n.Count(c => c == '/') == 1)
            .ToList();
        // A product with languages shares its voice.md between them; it isn't a mode of its own ({product}/voice).
        var shared = files.Where(f => LanguageOf(f) is not null).Select(SharedOf).ToHashSet(StringComparer.Ordinal);
        return files.Where(f => !shared.Contains(f));
    }

    /// <summary>
    /// Every agent's helpers in one of its folders, by their addresses: <c>agents/acme/subagents/cook/voice.md</c> is
    /// <c>acme.cook</c>, and so is <c>agents/acme/lookups/cook/voice.md</c> among the lookup agents.
    /// </summary>
    private static IEnumerable<string> Helpers(string folder) =>
        SkillFileSource.EmbeddedFiles
            .Select(file => file.Split('/'))
            .Where(parts => parts is [var agents, _, var kind, _, var voice]
                            && agents == Agents && kind == folder && voice == Voice)
            .Select(parts => $"{parts[1]}.{parts[3]}");

    /// <summary>
    /// A helper's file from its address, inside its agent in <paramref name="folder"/>; a name without its identity
    /// points at no file, so it is refused as unknown.
    /// </summary>
    private static string HelperFile(string address, string folder) => address.Split('.') is [var identity, var name]
        ? $"{Agents}/{identity}/{folder}/{name}/{Voice}"
        : $"{Agents}/{folder}/{address}/{Voice}";

    /// <summary>A language mode: matches <c>acme/voice-af</c> (code <c>af</c>), not <c>acme/voice</c>.</summary>
    [GeneratedRegex("^[a-z0-9-]+/voice-(?<code>[a-z]{2})$")]
    private static partial Regex LanguageMode();

    /// <summary>
    /// A heading after the first line, since a mode file starts with its voice's: matches the <c># </c> of
    /// <c>"# Voice\n# Routing"</c> at its second line, not at its first.
    /// </summary>
    [GeneratedRegex(@"(?<=\n)# ", RegexOptions.None)]
    private static partial Regex SecondHeading();

    /// <summary>
    /// A role's name: matches <c>acme.cook</c>, <c>acme-page-lookup</c> and <c>acme/voice-en</c>, not
    /// <c>../acme</c> or <c>a.b.c</c>.
    /// </summary>
    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*([/.][a-z0-9]+(-[a-z0-9]+)*)?$")]
    private static partial Regex NamePattern();
}
