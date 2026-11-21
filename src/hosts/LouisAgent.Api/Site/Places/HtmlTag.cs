using System.Net;

namespace LouisAgent.Api.Site.Places;

/// <summary>
/// One opening tag of a rendered page (F127.7): its name, its attributes, where it starts and where its own markup
/// ends. Read by plain string reading, since the markup is our own components' (ADR 0057: a regular expression is the
/// last option).
/// </summary>
/// <param name="Name">The element's name, lower case.</param>
/// <param name="Attributes">Its attributes by name; a bare one, such as <c>data-piece</c>, has an empty value.</param>
/// <param name="Start">Where the tag's <c>&lt;</c> is.</param>
/// <param name="End">Just past the tag's <c>&gt;</c>, where the element's content starts.</param>
internal sealed record HtmlTag(string Name, IReadOnlyDictionary<string, string> Attributes, int Start, int End)
{
    /// <summary>Its id, or null.</summary>
    public string? Id => Attributes.GetValueOrDefault("id");

    /// <summary>Whether it carries the attribute.</summary>
    public bool Has(string attribute) => Attributes.ContainsKey(attribute);

    /// <summary>Whether its class list holds the class.</summary>
    public bool HasClass(string name) =>
        Attributes.GetValueOrDefault("class")?.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains(name)
        ?? false;

    /// <summary>Every opening tag in the markup, in order; closing tags, comments and doctypes are skipped.</summary>
    public static IEnumerable<HtmlTag> All(string html)
    {
        int at = html.IndexOf('<');
        while (at >= 0 && at + 1 < html.Length)
        {
            int end = TagEnd(html, at);
            if (end < 0) yield break;
            if (char.IsAsciiLetter(html[at + 1])) yield return Read(html, at, end);
            at = html.IndexOf('<', end);
        }
    }

    /// <summary>
    /// Where this element's markup ends: before its own closing tag, nested ones of its name counted.
    /// </summary>
    public int ContentEnd(string html)
    {
        int depth = 1, at = End;
        while (depth > 0)
        {
            int next = html.IndexOf('<', at);
            if (next < 0) return html.Length;
            int end = TagEnd(html, next);
            if (end < 0) return html.Length;
            string tag = html[(next + 1)..(end - 1)];
            if (tag.StartsWith('/') && NameOf(tag[1..]) == Name) depth--;
            else if (NameOf(tag) == Name && !tag.EndsWith('/')) depth++;
            if (depth == 0) return next;
            at = end;
        }

        return html.Length;
    }

    /// <summary>Just past the tag's closing <c>&gt;</c>, quotes skipped; -1 when it never closes.</summary>
    private static int TagEnd(string html, int start)
    {
        char? quote = null;
        for (int i = start + 1; i < html.Length; i++)
        {
            char c = html[i];
            if (quote is not null && c == quote) quote = null;
            else if (quote is null && c is '"' or '\'') quote = c;
            else if (quote is null && c == '>') return i + 1;
        }

        return -1;
    }

    /// <summary>A tag's name: its letters, digits and dashes from the start, lower case.</summary>
    private static string NameOf(string tag) =>
        new string(tag.TakeWhile(c => char.IsAsciiLetterOrDigit(c) || c == '-').ToArray()).ToLowerInvariant();

    /// <summary>The tag at this place, its attributes read one by one.</summary>
    private static HtmlTag Read(string html, int start, int end)
    {
        string inside = html[(start + 1)..(end - 1)].TrimEnd('/');
        string name = NameOf(inside);
        return new HtmlTag(name, AttributesOf(inside[name.Length..]), start, end);
    }

    /// <summary>The attributes, each a name with a quoted, a bare or no value.</summary>
    private static Dictionary<string, string> AttributesOf(string text)
    {
        var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        int i = 0;
        while (i < text.Length)
        {
            while (i < text.Length && char.IsWhiteSpace(text[i])) i++;
            int nameStart = i;
            while (i < text.Length && !char.IsWhiteSpace(text[i]) && text[i] != '=') i++;
            if (i == nameStart) break;
            string attribute = text[nameStart..i];
            attributes[attribute] = i < text.Length && text[i] == '=' ? ValueAt(text, ref i) : "";
        }

        return attributes;
    }

    /// <summary>The value after an <c>=</c>, quoted or bare, the position moved past it.</summary>
    private static string ValueAt(string text, ref int i)
    {
        i++;
        if (i >= text.Length) return "";
        char quote = text[i];
        if (quote is not ('"' or '\''))
        {
            int bare = i;
            while (i < text.Length && !char.IsWhiteSpace(text[i])) i++;
            return WebUtility.HtmlDecode(text[bare..i]);
        }

        int close = text.IndexOf(quote, i + 1);
        if (close < 0) close = text.Length;
        string value = text[(i + 1)..close];
        i = Math.Min(close + 1, text.Length);
        return WebUtility.HtmlDecode(value);
    }
}
