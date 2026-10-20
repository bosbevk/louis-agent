namespace LouisAgent.Core.Tools.Web.Fetching;

using System.Net;
using System.Text.RegularExpressions;

/// <summary>HTML as readable text (S, CL15, from WebTools): what FetchUrl, the page renderer and the search providers show.</summary>
internal static partial class HtmlText
{
    /// <summary>Converts HTML to readable text: main content only, markdown-style headings, lists, links and code.</summary>
    internal static (string? Title, string Text) ToText(string html, Uri baseUri)
    {
        // Pages (and files checked out on Windows) may use CRLF; keep the output to plain \n so code blocks stay clean.
        html = html.ReplaceLineEndings("\n");
        string? title = TitleTag().Match(html) is { Success: true } t ? CleanInline(t.Groups[1].Value) : null;

        // Prefer the main content region when the page marks one.
        string content = MainOrArticle().Match(html) is { Success: true } main ? main.Groups["body"].Value
            : BodyTag().Match(html) is { Success: true } body ? body.Groups[1].Value : html;

        content = Comment().Replace(content, "");
        content = NonContent().Replace(content, " ");

        // Code blocks keep their line breaks; park them so the whitespace cleanup below leaves them alone.
        var blocks = new List<string>();
        content = PreBlock().Replace(content, m =>
        {
            blocks.Add("\n```\n" + WebUtility.HtmlDecode(Tag().Replace(m.Groups[1].Value, "")).Trim('\n') + "\n```\n");
            return $"\u0001{blocks.Count - 1}\u0001";
        });

        content = Heading().Replace(content, m =>
            $"\n\n{new string('#', m.Groups[1].Value[0] - '0')} {CleanInline(m.Groups[2].Value)}\n\n");
        content = Anchor().Replace(content, m =>
        {
            string text = CleanInline(m.Groups["text"].Value);
            string href = WebUtility.HtmlDecode(m.Groups["href"].Value);
            if (text.Length == 0) return "";
            if (href.StartsWith('#') || href.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase) ||
                !Uri.TryCreate(baseUri, href, out var target) || target.Scheme is not ("http" or "https"))
                return text;
            return $"[{text}]({target})";
        });
        content = ListItem().Replace(content, "\n- ");
        content = BlockBreak().Replace(content, "\n");
        content = InlineCode().Replace(content, m => $"`{m.Groups[1].Value}`");
        content = WebUtility.HtmlDecode(Tag().Replace(content, ""));

        var lines = content.Split('\n').Select(l => Whitespace().Replace(l, " ").Trim());
        string text = BlankLines().Replace(string.Join('\n', lines), "\n\n").Trim();
        text = Placeholder().Replace(text, m => blocks[int.Parse(m.Groups[1].Value)]);
        return (title, BlankLines().Replace(text, "\n\n").Trim());
    }

    /// <summary>An inline fragment (a title, a snippet) as one line, tags gone and entities decoded.</summary>
    internal static string CleanInline(string? html) =>
        Whitespace().Replace(WebUtility.HtmlDecode(Tag().Replace(html ?? "", "")), " ").Trim();

    /// <summary>
    /// The whole body with every tag a space and only what a browser never shows gone (VerifyQuote, F30): a price per
    /// kilo can sit outside <c>main</c>, in a footer or a sidebar. Stripped with nothing between, a badge joins its price
    /// ("newr54.99"), which then isn't seen as a price (FX16, the PR #54 re-review).
    /// </summary>
    internal static string TagsAsSpaces(string html) => Tag().Replace(ScriptOrStyle().Replace(html, " "), " ");

    [GeneratedRegex(@"<title[^>]*>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex TitleTag();

    [GeneratedRegex(@"<(main|article)\b[^>]*>(?<body>.*)</\1>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex MainOrArticle();

    [GeneratedRegex(@"<body[^>]*>(.*)</body>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex BodyTag();

    [GeneratedRegex(@"<!--.*?-->", RegexOptions.Singleline)]
    private static partial Regex Comment();

    [GeneratedRegex(@"<(script|style|noscript|svg|nav|footer|aside|form|iframe|template|button)\b[^>]*>.*?</\1>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex NonContent();

    [GeneratedRegex(@"<(script|style|noscript|template)\b[^>]*>.*?</\1>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ScriptOrStyle();

    [GeneratedRegex(@"<pre[^>]*>(.*?)</pre>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex PreBlock();

    [GeneratedRegex(@"<h([1-6])[^>]*>(.*?)</h\1>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex Heading();

    [GeneratedRegex(@"<a\b[^>]*href=""(?<href>[^""]*)""[^>]*>(?<text>.*?)</a>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex Anchor();

    [GeneratedRegex(@"<li\b[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex ListItem();

    // No </li>: each <li> already starts a new "- " line.
    [GeneratedRegex(@"<(br|/p|p|/div|div|/tr|tr|/ul|/ol|/table|/section|section|/blockquote|blockquote|hr)\b[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex BlockBreak();

    [GeneratedRegex(@"<code[^>]*>(.*?)</code>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex InlineCode();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex Tag();

    [GeneratedRegex(@"[ \t\r\f\v ]+")]
    private static partial Regex Whitespace();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex BlankLines();

    [GeneratedRegex("\u0001(\\d+)\u0001")]
    private static partial Regex Placeholder();
}
