using LouisAgent.Choices;
using LouisAgent.Web.Components.Areas.Themes;

namespace LouisAgent.Web.Testing.Styles;

/// <summary>
/// Holds a theme's stylesheet to the framework's names (F151.1). A theme is only values, so it must set every name
/// the framework sets, once in its dark block and once in its light one, or the framework's own value shows through
/// in that style. This reads the names out of the framework's plain <c>:root</c> blocks, and says which of them a
/// theme's block for one style leaves unset. It reads the CSS as text with plain string methods, so any theme's
/// tests can point it at that theme's sheet.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0124-a-sites-look-is-a-theme-and-a-style.md">ADR 0124: a site's look is a theme and a style</seealso>
public static class ThemeSheet
{
    /// <summary>The selector the framework's names are set under.</summary>
    private const string Root = ":root";

    /// <summary>What starts a custom property's name, such as <c>--web-ink</c>.</summary>
    private const string NameStart = "--";

    /// <summary>Every name the framework's plain <c>:root</c> blocks set, each once, in the order they come.</summary>
    public static IReadOnlyList<string> NamesToSet(string frameworkCss) =>
        [.. Bodies(frameworkCss, Root).SelectMany(Sets).Distinct()];

    /// <summary>
    /// The names a theme's block for one style leaves unset, in the order given. With no block for that theme and
    /// style in the sheet, that is every name.
    /// </summary>
    public static IReadOnlyList<string> Missing(
        string css, SiteTheme theme, SiteStyle style, IReadOnlyList<string> names)
    {
        var set = Bodies(css, Selector(theme, style)).SelectMany(Sets).ToHashSet();
        return [.. names.Where(name => !set.Contains(name))];
    }

    /// <summary>
    /// The selector a theme's block for one style is written under, such as
    /// <c>:root[data-theme="glass"][data-style="dark"]</c>, with the attributes <see cref="SiteLook"/> names.
    /// </summary>
    public static string Selector(SiteTheme theme, SiteStyle style) =>
        $"{Root}[{SiteLook.ThemeAttribute}=\"{theme.Id}\"][{SiteLook.StyleAttribute}=\"{style.Wire()}\"]";

    /// <summary>What every block whose selector is exactly this one holds between its braces.</summary>
    private static IEnumerable<string> Bodies(string css, string selector)
    {
        for (int at = css.IndexOf(selector, StringComparison.Ordinal);
             at >= 0;
             at = css.IndexOf(selector, at + selector.Length, StringComparison.Ordinal))
        {
            if (BodyAt(css, at, selector) is { } body) yield return body;
        }
    }

    /// <summary>
    /// The block's body when the selector found here is the whole selector: it starts the sheet or a rule, and only
    /// space stands between it and its brace. <c>:root</c> inside <c>:root[data-theme="glass"]</c> is not one.
    /// </summary>
    private static string? BodyAt(string css, int at, string selector)
    {
        if (at > 0 && !StartsARule(css[at - 1])) return null;

        int open = at + selector.Length;
        while (open < css.Length && char.IsWhiteSpace(css[open])) open++;
        if (open == css.Length || css[open] != '{') return null;

        int close = css.IndexOf('}', open);
        return close < 0 ? null : css[(open + 1)..close];
    }

    /// <summary>What may stand just before a rule's selector: space, or the end of the rule before it.</summary>
    private static bool StartsARule(char before) => char.IsWhiteSpace(before) || before is '}' or ';';

    /// <summary>The names one block sets, such as <c>--web-ink</c> from <c>--web-ink: #111;</c>.</summary>
    private static IEnumerable<string> Sets(string body) =>
        body.Split(';')
            .Select(declaration => declaration.Trim())
            .Where(declaration => declaration.StartsWith(NameStart, StringComparison.Ordinal))
            .Where(declaration => declaration.Contains(':'))
            .Select(declaration => declaration[..declaration.IndexOf(':')].Trim());
}
