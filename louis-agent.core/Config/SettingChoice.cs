namespace LouisAgent.Core.Config;

/// <summary>
/// A setting's choice as its enum (F32 step 1b; ADR 0024: choices are enums, not strings). The one parser for a value
/// that names one of a fixed set (a provider, a renderer, a freshness): its name with the case ignored, or an alias,
/// read once where the setting enters; code then switches on the enum. Never a number: <see cref="Enum.TryParse{T}(string, bool, out T)"/>
/// takes "1", which names nothing anyone wrote. The names a "supported" message lists come from the enum itself, so
/// they can't drift from what the code accepts.
/// </summary>
public static class SettingChoice
{
    /// <summary>
    /// False only for a value that names nothing. A value that isn't set is no choice (<paramref name="choice"/> null,
    /// true), so a caller can fall back to its default.
    /// </summary>
    public static bool TryParse<T>(string? value, out T? choice, IReadOnlyDictionary<string, T>? aliases = null) where T : struct, Enum
    {
        choice = null;
        string text = value?.Trim() ?? "";
        if (text.Length == 0) return true;

        foreach (var member in Enum.GetValues<T>())
            if (string.Equals(member.ToString(), text, StringComparison.OrdinalIgnoreCase))
            {
                choice = member;
                return true;
            }

        var alias = aliases?.FirstOrDefault(a => string.Equals(a.Key, text, StringComparison.OrdinalIgnoreCase));
        if (alias is { Key: not null } found)
        {
            choice = found.Value;
            return true;
        }
        return false;
    }

    /// <summary>The enum's names in lower case, in the order it declares them: what a "supported" message lists.</summary>
    public static IReadOnlyList<string> Names<T>() where T : struct, Enum =>
        Enum.GetNames<T>().Select(n => n.ToLowerInvariant()).ToArray();
}
