using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Xml;

namespace LouisAgent.Packs.Recipes.Recipes;

/// <summary>
/// The small readers that schema.org's loose shapes need: a field that may be a text, a number, an object or a list
/// of them, and a duration. Each gives back what the page said, or null when it said nothing usable.
/// </summary>
/// <remarks>
/// Adapter, kept apart from <see cref="SchemaOrgRecipe"/>: the mapper says which field becomes what, and this says
/// how to read one loose value, so the "string, object or list" rule is written once and not once per field.
/// </remarks>
internal static class SchemaOrgValues
{
    private static readonly string[] LineBreakingTags = ["br", "p", "div", "li"];

    /// <summary>
    /// The property of an object, or null when the value isn't an object or doesn't have it.
    /// </summary>
    public static JsonElement? Property(JsonElement? node, string name) =>
        node is { ValueKind: JsonValueKind.Object } o && o.TryGetProperty(name, out var value) ? value : null;

    /// <summary>
    /// The first usable text of a value: the text itself, a number as text, or the first one of a list. For an
    /// object, the text of the property you name (an author's <c>name</c>, an image's <c>url</c>), or nothing.
    /// </summary>
    /// <remarks>
    /// A switch expression on the value's shape, one arm per shape, so no condition sits inside another (ADR 0051).
    /// </remarks>
    public static string? Text(JsonElement? value, string? objectKey = null, bool keepTags = false) => value switch
    {
        { ValueKind: JsonValueKind.String } v => NonBlank(v.GetString(), keepTags),
        { ValueKind: JsonValueKind.Number } v => v.TryGetDecimal(out var n)
            ? n.ToString(CultureInfo.InvariantCulture)
            : null,
        { ValueKind: JsonValueKind.Array } v => Texts(v, objectKey, keepTags).FirstOrDefault(),
        { ValueKind: JsonValueKind.Object } v when objectKey is not null =>
            Text(Property(v, objectKey), objectKey, keepTags),
        _ => null
    };

    /// <summary>
    /// Every usable text of a value, in order: one for a text, one per item for a list; blanks are left out.
    /// </summary>
    public static IEnumerable<string> Texts(JsonElement? value, string? objectKey = null, bool keepTags = false) =>
        value switch
        {
            { ValueKind: JsonValueKind.Array } v => v.EnumerateArray()
                .Select(item => Text(item, objectKey, keepTags)).OfType<string>(),
            _ => Text(value, objectKey, keepTags) is { } text ? [text] : []
        };

    /// <summary>
    /// A time from its ISO 8601 duration ("PT1H15M"). Text that isn't one ("PT", "P", words) is no time at all.
    /// </summary>
    public static TimeSpan? Duration(JsonElement? value)
    {
        if (Text(value) is not { } text) return null;

        try
        {
            return XmlConvert.ToTimeSpan(text);
        }
        catch (Exception e) when (e is FormatException or OverflowException)
        {
            // Pages write "PT" or "P" when they have no time; that is unknown, not an error.
            return null;
        }
    }

    /// <summary>
    /// A web address from the text, made absolute against the page it was read from. Only http and https count.
    /// </summary>
    public static Uri? Address(string? text, Uri page) =>
        text is not null && Uri.TryCreate(page, text, out var uri) && uri.Scheme is "http" or "https" ? uri : null;

    /// <summary>
    /// The text with its HTML entities read (<c>&amp;quot;</c> is a quote), its tags taken out and trimmed, or null
    /// when nothing is left. Every text of the recipe comes through here, so each is decoded once, and the tags go
    /// after the entities because a page can write <c>&amp;lt;b&amp;gt;</c>. A caller that needs the tags to tell a
    /// heading from a line asks to keep them and takes them out itself.
    /// </summary>
    private static string? NonBlank(string? text, bool keepTags)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        var decoded = WebUtility.HtmlDecode(text);
        var read = keepTags ? decoded : WithoutTags(decoded);
        return string.IsNullOrWhiteSpace(read) ? null : read.Trim();
    }

    /// <summary>
    /// The text without its HTML tags. A tag is a <c>&lt;</c> followed by a letter or a <c>/</c>, up to the next
    /// <c>&gt;</c>; any other <c>&lt;</c> ("cook for &lt; 5 min") is the page's own and stays, and so does a tag that
    /// is never closed. A tag that breaks the line leaves a space, so "Mix.&lt;br&gt;Bake." stays two sentences.
    /// </summary>
    /// <remarks>
    /// A plain walk along the text and not a regular expression (ADR 0057): one rule, read in one pass.
    /// </remarks>
    public static string WithoutTags(string text)
    {
        var result = new StringBuilder(text.Length);
        var position = 0;
        while (position < text.Length)
        {
            var end = TagEnd(text, position);
            if (end < 0)
            {
                result.Append(text[position++]);
                continue;
            }

            if (BreaksTheLine(text[(position + 1)..end]) && result.Length > 0 && !char.IsWhiteSpace(result[^1]))
                result.Append(' ');
            position = end + 1;
        }

        return result.ToString();
    }

    /// <summary>Where the tag starting at this position ends (its <c>&gt;</c>), or -1 when none starts here.</summary>
    private static int TagEnd(string text, int start)
    {
        if (text[start] != '<' || start + 1 >= text.Length) return -1;
        if (!char.IsLetter(text[start + 1]) && text[start + 1] != '/') return -1;

        return text.IndexOf('>', start + 1);
    }

    /// <summary>A browser starts a new line at these tags, so the words either side are never one word.</summary>
    private static bool BreaksTheLine(string tag)
    {
        var name = new string(tag.TrimStart('/').TakeWhile(char.IsLetter).ToArray());
        return LineBreakingTags.Contains(name, StringComparer.OrdinalIgnoreCase);
    }
}
