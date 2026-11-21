using System.Reflection;
using LouisAgent.Composition.Messages;

namespace LouisAgent.Testing.Messages;

/// <summary>
/// Every message member whose <c>[Holes]</c> record doesn't match its template, one line each; empty when they all do
/// (ADR 0119). Shared, so core's tests and each pack's hold their own message enums to it.
/// </summary>
public static class MessageHoles
{
    /// <summary>The members of <paramref name="messageEnums"/> whose holes record and template disagree.</summary>
    public static IReadOnlyList<string> Problems(IEnumerable<Type> messageEnums) =>
        messageEnums.SelectMany(type => Enum.GetValues(type).Cast<Enum>()).Select(Problem).OfType<string>().ToList();

    /// <summary>One member's problem: its record's properties aren't its template's holes; none without a record.</summary>
    private static string? Problem(Enum member)
    {
        var holes = member.GetType().GetField(member.ToString())?.GetCustomAttribute<HolesAttribute>();
        if (holes is null) return null;

        var wanted = MessageText.Holes(MessageText.Template(member)).Distinct().Order().ToList();
        var given = holes.Record.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.Name).Order().ToList();
        if (wanted.SequenceEqual(given)) return null;
        return $"{member.GetType().Name}.{member}: the template's holes are {string.Join(", ", wanted)}; " +
               $"{holes.Record.Name} has {string.Join(", ", given)}";
    }
}
