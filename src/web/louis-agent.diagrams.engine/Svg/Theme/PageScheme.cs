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
}
