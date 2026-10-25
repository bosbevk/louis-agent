namespace LouisAgent.Core.Messages;

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
using System.Text.RegularExpressions;

/// <summary>
/// The text of every message, kept in message enums: the shared <see cref="ErrorMessage"/>, <see cref="ToolMessage"/> and
/// <see cref="UserText"/>, and an area's <c>{Area}ToolMessage</c>, <c>{Area}LogMessage</c> or <c>{Area}ErrorMessage</c>. Each member's
/// <see cref="DescriptionAttribute"/> is a template with named holes, e.g. <c>"Unknown LLM_CACHE_TTL '{Value}'. Use 5m or 1h."</c>
/// (docs/CLAUDE.md, *Messages and logging*).
/// </summary>
public static partial class MessageText
{
    private static readonly ConcurrentDictionary<Enum, string> Templates = new();

    /// <summary>The member's template, holes unfilled: for a structured logger, which fills them itself.</summary>
    public static string Template(Enum message) => Templates.GetOrAdd(message, Read);

    /// <summary>
    /// The member's text with its holes filled from <paramref name="values"/>' properties of the same names, e.g.
    /// <c>Format(ConfigErrorMessage.UnknownCacheTtl, new { Value = "10m" })</c>. A hole without a value throws, so a message never
    /// goes out with a gap in it.
    /// </summary>
    public static string Format(Enum message, object? values = null)
    {
        string template = Template(message);
        return Hole().Replace(template, match =>
        {
            string name = match.Groups[1].Value;
            PropertyInfo? property = values?.GetType().GetProperty(name);
            if (property is null)
                throw new ArgumentException(Format(ErrorMessage.MessageNeedsValue, new { Message = Name(message), Hole = $"{{{name}}}" }),
                    nameof(values));
            return Convert.ToString(property.GetValue(values), System.Globalization.CultureInfo.InvariantCulture) ?? "";
        });
    }

    /// <summary>The names of a template's holes, in order.</summary>
    public static IReadOnlyList<string> Holes(string template) =>
        Hole().Matches(template).Select(m => m.Groups[1].Value).ToList();

    private static string Read(Enum message) =>
        message.GetType().GetField(message.ToString())?.GetCustomAttribute<DescriptionAttribute>()?.Description
        ?? throw new InvalidOperationException(Format(ErrorMessage.MessageHasNoTemplate, new { Message = Name(message) }));

    private static string Name(Enum message) => $"{message.GetType().Name}.{message}";

    /// <summary>A named hole: <c>{Name}</c>, a letter first, then letters and digits.</summary>
    [GeneratedRegex(@"\{([A-Za-z][A-Za-z0-9]*)\}")]
    private static partial Regex Hole();
}