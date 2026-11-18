namespace LouisAgent.Diagrams.Engine.Svg.Theme;

/// <summary>
/// How a picture picks its light or dark colours. Read as a file it follows the device's setting. Drawn into one of
/// our pages it follows the page's own Dark and Light switch, which the page says in an attribute on its
/// <c>&lt;html&gt;</c>.
/// </summary>
/// <remarks>
/// The engine names the attribute and its two words itself, since a library the docs' tool uses must not depend on
/// the framework's web for them. A test in the web's tests, which sees both, holds them equal to the page's.
/// </remarks>
public static class PageScheme
{
    /// <summary>The attribute our pages carry on their <c>&lt;html&gt;</c> to say which style is on.</summary>
    public const string Attribute = "data-style";

    /// <summary>What the attribute says for a dark page.</summary>
    public const string Dark = "dark";

    /// <summary>What the attribute says for a light page.</summary>
    public const string Light = "light";

    /// <summary>
    /// The rules that follow a style's light colours: the device's rule for a dark screen, then one for a page that
    /// says dark and one for a page that says light, which win where both apply.
    /// </summary>
    internal static string Rules(string light, string dark) =>
        $$"""
          @media (prefers-color-scheme: dark) {
            svg { {{dark}} }
          }
          [{{Attribute}}="{{Dark}}"] svg { {{dark}} }
          [{{Attribute}}="{{Light}}"] svg { {{light}} }
          """;

    /// <summary>The attribute our pages carry on their <c>&lt;html&gt;</c> to say which theme is on (F161.1).</summary>
    public const string ThemeAttribute = "data-theme";

    /// <summary>What the theme's attribute says for the flat theme.</summary>
    public const string Flat = "flat";

    /// <summary>What the theme's attribute says for the glass theme.</summary>
    public const string Glass = "glass";

    /// <summary>
    /// The rules that give a page of one theme its own colours, light and dark (F161.1, Louis's pick C): they name
    /// the theme and the style together, so they win over the style alone wherever both are said. They live only in a
    /// page's copy, inside its <c>@scope</c>, where the drawing itself is <c>:scope</c>: a bare <c>svg</c> there would
    /// mean an svg inside it.
    /// </summary>
    internal static string ThemeRules(string theme, string light, string dark) =>
        $$"""
          [{{ThemeAttribute}}="{{theme}}"][{{Attribute}}="{{Light}}"] :scope { {{light}} }
          [{{ThemeAttribute}}="{{theme}}"][{{Attribute}}="{{Dark}}"] :scope { {{dark}} }
          """;
}
