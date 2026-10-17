namespace LouisAgent.Core.Skills;

using System.Text.Json;

/// <summary>
/// The minion roles (level 2, F30) and the tools each accepts, from the built-in <c>minions/minions.json</c>:
/// <c>{ "roles": [ { "role": "puls-minion-web-price-lookup", "tools": ["FetchUrl", "ReadRenderedPage"] } ] }</c>. Data,
/// not code, so a new minion is a role file beside it and one line here. The list can only narrow what a specialist
/// hands down: the API checks the parent's own tools too, and never a delegation or self-extension tool. Read once from
/// the assembly, like every built-in prompt (ADR 0018); a file that isn't its shape lists no role, so no minion opens.
/// </summary>
public static class MinionRoles
{
    /// <summary>The data file, beside the roles' files.</summary>
    public const string File = Personalities.MinionsFolder + "/minions.json";

    private static readonly Lazy<IReadOnlyDictionary<string, IReadOnlyList<string>>> Loaded =
        new(() => Parse(SkillFileSource.ReadEmbedded(File) ?? string.Empty));

    /// <summary>Every minion role the file lists, sorted.</summary>
    public static IReadOnlyList<string> Roles => Loaded.Value.Keys.Order(StringComparer.Ordinal).ToList();

    /// <summary>The tools <paramref name="role"/> accepts, or null when it is no minion role (a specialist's, a base, unlisted).</summary>
    public static IReadOnlyList<string>? ToolsOf(string role) => Loaded.Value.GetValueOrDefault(role);

    /// <summary>
    /// Parses the file's text: role → its tools, roles in the shape <c>{product}-minion-{function}</c> only (a base is
    /// no role). Empty when the text isn't the file's shape.
    /// </summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> Parse(string json)
    {
        var roles = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        try
        {
            using var document = JsonDocument.Parse(json);
            foreach (var entry in document.RootElement.GetProperty("roles").EnumerateArray())
            {
                string? role = entry.GetProperty("role").GetString();
                if (!Personalities.IsValidName(role) || Personalities.MinionBaseOf(role!) is null) continue;
                roles[role!] = entry.GetProperty("tools").EnumerateArray()
                    .Select(t => t.GetString()).OfType<string>().Where(t => t.Length > 0)
                    .Distinct(StringComparer.Ordinal).ToList();
            }
            return roles;
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            return new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        }
    }
}
