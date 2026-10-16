namespace LouisAgent.Core.Skills;

using System.Text.Json;
using System.Text.RegularExpressions;

/// <summary>
/// Personalities a session can choose (F21), in the folder of the level that reads them (CL8, ADR 0016):
/// <c>agent/{name}.md</c> (level 0, a voice), <c>specialists/{name}.md</c> (level 1, a role),
/// <c>minions/{name}.md</c> (level 2, F30), under core's <c>Prompts/</c> and read only from the assembly (CL8
/// step 4). <see cref="DefaultFile"/> is used when a session names none. A name is lowercase letters, digits and
/// dashes, so it never reaches outside its folder.
/// </summary>
public static partial class Personalities
{
    /// <summary>The personality of a session that names none: the one every host had before F21.</summary>
    public const string DefaultFile = AgentFolder + "/personality.md";

    /// <summary>Level 0's folder, under the skills folder: the voices of the agent the user talks to.</summary>
    public const string AgentFolder = "agent";

    /// <summary>Level 1's folder: one role per specialist, and <see cref="RolesFile"/>.</summary>
    public const string SpecialistsFolder = "specialists";

    /// <summary>Level 2's folder: one function per minion (F30).</summary>
    public const string MinionsFolder = "minions";

    private const int MaxNameLength = 40;

    /// <summary>The file of the rules that give a sub-session its role from its toolsets (<see cref="RoleFor"/>).</summary>
    public const string RolesFile = SpecialistsFolder + "/roles.json";

    /// <summary>The folder, under the skills folder, of <paramref name="level"/>'s personalities.</summary>
    public static string FolderOf(SkillLevel level) => level switch
    {
        SkillLevel.Agent => AgentFolder,
        SkillLevel.Specialist => SpecialistsFolder,
        SkillLevel.Minion => MinionsFolder,
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, null),
    };

    /// <summary>
    /// The role for a sub-session with <paramref name="toolsets"/> when its parent named none (the delegation check
    /// found Puls passing a role in one delegation of four): the first rule in <paramref name="rolesJson"/> whose
    /// toolsets the session has, in the file's order (so files + python is the analyst), else its default; null when the
    /// file can't be read or names no role.
    /// </summary>
    public static string? RoleFor(string rolesJson, IEnumerable<string> toolsets)
    {
        var loaded = toolsets.Select(t => t.Trim().ToLowerInvariant()).ToHashSet();
        try
        {
            using var document = JsonDocument.Parse(rolesJson);
            var root = document.RootElement;
            foreach (var rule in root.GetProperty("roles").EnumerateArray())
                if (rule.GetProperty("toolsets").EnumerateArray().Any(t => loaded.Contains(t.GetString()?.ToLowerInvariant() ?? "")))
                    return rule.GetProperty("personality").GetString();
            return root.TryGetProperty("default", out var fallback) ? fallback.GetString() : null;
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            return null;
        }
    }

    /// <summary>Whether <paramref name="name"/> can name a personality (and so can be turned into a path safely).</summary>
    public static bool IsValidName(string? name) => name is { Length: > 0 and <= MaxNameLength } && NamePattern().IsMatch(name);

    /// <summary>
    /// Whether <paramref name="file"/> shares level 0's folder without being a voice to choose: personality.md (a session
    /// that names none) and default.md (the router).
    /// </summary>
    internal static bool IsReserved(string file) => file is DefaultFile or SkillFiles.Router;

    /// <summary>The skill file of a named personality at <paramref name="level"/>, relative to the skills folder.</summary>
    public static string FileName(string name, SkillLevel level) => $"{FolderOf(level)}/{name}.md";

    /// <summary>The named personalities at <paramref name="level"/>, all built in (CL8 step 4), sorted.</summary>
    public static IReadOnlyList<string> Available(SkillLevel level) =>
        SkillFileSource.Files(FolderOf(level))
            .Where(f => f.EndsWith(".md", StringComparison.Ordinal))
            .Select(f => f[..^".md".Length])
            .Where(n => IsValidName(n) && !IsReserved(FileName(n, level)))
            .ToList();

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex NamePattern();
}
