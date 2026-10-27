using System.Reflection;
using System.Text.Json.Serialization;

namespace LouisAgent.Choices;

/// <summary>
/// A setting's choice as its enum (F32 step 1b; ADR 0024: choices are enums, not strings). The one parser for a value
/// that names one of a fixed set (a provider, a renderer, a freshness): its name with the case ignored, its wire name
/// (F18 step 7.5, ADR 0043) or an alias, read once where the setting enters; code then switches on the enum. Never a number:
/// <see cref="Enum.TryParse{T}(string, bool, out T)"/> takes "1", which names nothing anyone wrote. The names a "supported"
/// message lists come from the enum itself, so they can't drift from what the code accepts. A member that travels as text
/// other than its name (<c>fast-mode</c> for <c>FastMode</c>) says so with .NET's own <see cref="JsonStringEnumMemberNameAttribute"/>, the one the JSON writer reads too, so parsing
/// and JSON cannot disagree; <see cref="Wire{T}"/> writes it back.
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

        foreach (var (member, wire) in Table<T>.Members)
            if (string.Equals(member.ToString(), text, StringComparison.OrdinalIgnoreCase) || string.Equals(wire, text, StringComparison.OrdinalIgnoreCase))
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

    /// <summary>The text a member travels as (F18 step 7.5): its <see cref="JsonStringEnumMemberNameAttribute"/>, else its own name in lower case. A value the enum does not define (<c>(Mode)99</c>) is an <see cref="ArgumentOutOfRangeException"/> naming the enum and the value.</summary>
    public static string Wire<T>(T member) where T : struct, Enum
    {
        foreach (var (known, wire) in Table<T>.Members)
            if (EqualityComparer<T>.Default.Equals(known, member)) return wire;
        throw new ArgumentOutOfRangeException(nameof(member), member, ChoiceText.Format(ChoiceMessage.ValueNotDefined, typeof(T).Name, member));
    }

    /// <summary>The enum's wire names, in the order it declares them: what a "supported" message lists.</summary>
    public static IReadOnlyList<string> Names<T>() where T : struct, Enum => [.. Table<T>.Members.Select(m => m.Wire)];

    /// <summary>One enum's members with the text each travels as, read once per enum type.</summary>
    private static class Table<T> where T : struct, Enum
    {
        public static readonly IReadOnlyList<(T Member, string Wire)> Members =
            [.. Enum.GetValues<T>().Select(member => (member, typeof(T).GetField(member.ToString())!.GetCustomAttribute<JsonStringEnumMemberNameAttribute>()?.Name ?? member.ToString().ToLowerInvariant()))];
    }
}
