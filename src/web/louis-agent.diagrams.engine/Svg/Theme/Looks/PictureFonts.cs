namespace LouisAgent.Diagrams.Engine.Svg.Theme.Looks;

/// <summary>
/// The fonts every picture is written in (F160.1, Louis: "all diagrams must use font from website", "it is like the
/// framework defaults"): on a page, the site's own sans and mono, read from the settings the page already has, so a
/// picture's words and the site's brand are one font; drawn as a file, the framework's defaults, Geist and Geist Mono.
/// </summary>
/// <remarks>
/// The framework's lists are named once here, as the framework's web sets them, and a test holds the two equal, so a
/// change of font on the web reaches the pictures or fails a test (ADR 0123).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0123-ask-the-type.md">ADR 0123: ask the type</seealso>
internal static class PictureFonts
{
    /// <summary>The framework's sans, as its web sets <c>--bs-font-sans-serif</c>.</summary>
    private const string FrameworkSans = "\"Geist\", system-ui, -apple-system, \"Segoe UI\", sans-serif";

    /// <summary>The framework's mono, as its web sets <c>--bs-font-monospace</c>.</summary>
    private const string FrameworkMono = "\"Geist Mono\", ui-monospace, Menlo, Consolas, monospace";

    /// <summary>The picture's sans: the site's, else the framework's.</summary>
    public const string Sans = $"var(--bs-font-sans-serif, {FrameworkSans})";

    /// <summary>The picture's mono: the site's, else the framework's.</summary>
    public const string Mono = $"var(--bs-font-monospace, {FrameworkMono})";
}
