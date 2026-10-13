namespace LouisAgent.Core.Skills;

using System.Text.Json;
using System.Text.RegularExpressions;

/// <summary>
/// Personalities a session can choose (F21): <c>Skills/personalities/{name}.md</c>, embedded like the other built-in
/// skill files (a file on disk overrides). <see cref="DefaultFile"/> is used when a session names none. A name is
/// lowercase letters, digits and dashes, so it never reaches outside the folder.
/// </summary>
public static partial class Personalities
{
    /// <summary>The personality of a session that names none: the one every host had before F21.</summary>
    public const string DefaultFile = "personality.md";

    /// <summary>The folder, under the skills folder, that holds the named personalities.</summary>
    public const string Folder = "personalities";

    private const int MaxNameLength = 40;

    /// <summary>The file of the rules that give a sub-session its role from its toolsets (<see cref="RoleFor"/>).</summary>
    public const string RolesFile = Folder + "/roles.json";

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

    /// <summary>The skill file of a named personality, relative to the skills folder.</summary>
    public static string FileName(string name) => $"{Folder}/{name}.md";

    /// <summary>The named personalities: the built-in ones and any in <paramref name="skillsDirectory"/>, sorted.</summary>
    public static IReadOnlyList<string> Available(string? skillsDirectory)
    {
        var names = SkillFileSource.EmbeddedNames
            .Where(n => n.StartsWith(Folder + ".", StringComparison.Ordinal) && n.EndsWith(".md", StringComparison.Ordinal))
            .Select(n => n[(Folder.Length + 1)..^".md".Length]);
        if (skillsDirectory is not null && Directory.Exists(Path.Combine(skillsDirectory, Folder)))
            names = names.Concat(Directory.EnumerateFiles(Path.Combine(skillsDirectory, Folder), "*.md").Select(f => Path.GetFileNameWithoutExtension(f)));
        return names.Where(IsValidName).Distinct().Order(StringComparer.Ordinal).ToList();
    }

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex NamePattern();
}
