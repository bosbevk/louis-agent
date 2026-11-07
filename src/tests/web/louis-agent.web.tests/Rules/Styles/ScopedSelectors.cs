namespace LouisAgent.Web.Tests.Rules.Styles;

/// <summary>
/// Reads a component's style file and its markup the way Blazor's CSS isolation sees them: which class each rule's
/// scope lands on, and which classes the component's own elements draw. A scoped rule matches only an element its
/// own component renders, so a rule whose scope lands on a class drawn by a child component, or by a component with no
/// element at all, reaches nothing (FX63). Plain string reading, no regular expression (ADR 0057).
/// </summary>
internal static class ScopedSelectors
{
    /// <summary>
    /// The class each rule's scope lands on: the last compound of a selector, or the one before <c>::deep</c>. A
    /// selector that starts with <c>::deep</c> lands on the component's own elements, whatever they are, and gives
    /// <c>""</c>; one whose compound names no class (an element) gives nothing.
    /// </summary>
    public static IEnumerable<(string Selector, string Subject)> Subjects(string scss) =>
        Preludes(WithoutComments(scss))
            .SelectMany(prelude => prelude.Split(','))
            .Select(selector => selector.Trim())
            .Select(selector => (selector, Subject(selector)))
            .Where(rule => rule.Item2 is not null)
            .Select(rule => (rule.selector, rule.Item2!));

    /// <summary>
    /// The class names the component's own (lowercase) elements carry, and whether one of them is written from code.
    /// </summary>
    public static (HashSet<string> Classes, bool Dynamic) Drawn(string markup, string code)
    {
        var values = ClassValues(markup).ToList();
        var classes = values.SelectMany(value => value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Concat(QuotedStrings(code).SelectMany(text => text.Split(' ', StringSplitOptions.RemoveEmptyEntries)));

        return ([.. classes], values.Any(value => value.TrimStart().StartsWith('@')));
    }

    /// <summary>Whether the markup draws any element of its own: without one, Blazor puts the scope nowhere.</summary>
    public static bool DrawsAnElement(string markup) =>
        WithoutRazorComments(markup).Split('<').Skip(1).Any(tag => tag.Length > 0 && char.IsLower(tag[0]));

    /// <summary>The class a selector's scope lands on (see <see cref="Subjects"/>), or null for an element.</summary>
    private static string? Subject(string selector)
    {
        int deep = selector.IndexOf("::deep", StringComparison.Ordinal);
        string scoped = deep < 0 ? selector : selector[..deep].Trim();
        if (scoped.Length == 0) return "";

        string compound = scoped.Replace('>', ' ').Replace('+', ' ').Replace('~', ' ')
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)[^1];
        int dot = compound.IndexOf('.');
        if (dot < 0) return null;

        return new string(compound[(dot + 1)..].TakeWhile(c => char.IsLetterOrDigit(c) || c is '-' or '_').ToArray());
    }

    /// <summary>Every rule's selector text at the top level or inside an at-rule such as <c>@media</c>.</summary>
    private static IEnumerable<string> Preludes(string css)
    {
        var stack = new Stack<bool>();
        int start = 0;
        for (int i = 0; i < css.Length; i++)
        {
            char c = css[i];
            if (c is ';' or '}') start = i + 1;
            if (c == '}' && stack.Count > 0) stack.Pop();
            if (c != '{') continue;

            string prelude = css[start..i].Trim();
            bool atRule = prelude.StartsWith('@');
            if (!atRule && stack.All(isAt => isAt)) yield return prelude;
            stack.Push(atRule);
            start = i + 1;
        }
    }

    /// <summary>
    /// The value of every <c>class="…"</c> on a lowercase element, Razor's <c>@(…)</c> inside it kept whole.
    /// </summary>
    private static IEnumerable<string> ClassValues(string markup)
    {
        const string attribute = "class=\"";
        for (int i = markup.IndexOf(attribute, StringComparison.Ordinal);
             i >= 0;
             i = markup.IndexOf(attribute, i + 1, StringComparison.Ordinal))
        {
            int open = markup.LastIndexOf('<', i);
            int start = i + attribute.Length, end = start, depth = 0;
            for (; end < markup.Length && (markup[end] != '"' || depth > 0); end++)
                depth += markup[end] switch { '(' => 1, ')' => -1, _ => 0 };

            bool onAnElement = open >= 0 && open + 1 < markup.Length && char.IsLower(markup[open + 1]);
            if (onAnElement) yield return markup[start..end];
        }
    }

    /// <summary>The string literals in code-behind, where a component may build its classes.</summary>
    private static IEnumerable<string> QuotedStrings(string code) =>
        code.Split('"').Where((_, index) => index % 2 == 1);

    /// <summary>CSS without its comments, so a commented-out rule isn't read.</summary>
    private static string WithoutComments(string css) => Without(css, "/*", "*/");

    /// <summary>Markup without Razor's comments, so a tag named in one isn't counted.</summary>
    private static string WithoutRazorComments(string markup) => Without(markup, "@*", "*@");

    /// <summary>The text with every span from <paramref name="open"/> to <paramref name="close"/> cut out.</summary>
    private static string Without(string text, string open, string close)
    {
        var kept = new System.Text.StringBuilder();
        int at = 0;
        for (int start = text.IndexOf(open, StringComparison.Ordinal);
             start >= 0;
             start = text.IndexOf(open, at, StringComparison.Ordinal))
        {
            kept.Append(text, at, start - at);
            int end = text.IndexOf(close, start + open.Length, StringComparison.Ordinal);
            at = end < 0 ? text.Length : end + close.Length;
        }

        return kept.Append(text, at, text.Length - at).ToString();
    }
}
