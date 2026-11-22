namespace LouisAgent.Web.Tests.Rules.Styles.Surfaces.Depth;

/// <summary>
/// F169.4: the two windows that open over a page and the talk in a chat take their depth from the theme, so a theme
/// that says none for a shadow or a blur is obeyed there too. Each place reads a name the themes already set (Louis's
/// pick "A: the names there are", 2026-11-22), and the surface tool's mixins they call write no number of their own.
/// </summary>
public class DepthIsTheThemesTests
{
    /// <summary>The framework's web styles, which hold the tool and the sheets that call it.</summary>
    private static readonly string Styles = Path.Combine(TestPaths.RepoRoot, "src/web/LouisAgent.Web/Styles/web");

    /// <summary>The text of one of the framework's style sheets, by its path under the web styles.</summary>
    private static string Sheet(string path) => File.ReadAllText(Path.Combine(Styles, path));

    /// <summary>
    /// Whether a sheet holds a piece of text, said by the sheet's name and the text when it doesn't, rather than by
    /// the whole sheet.
    /// </summary>
    private static void Holds(string path, string text) =>
        Assert.That(Sheet(path).Contains(text, StringComparison.Ordinal), Is.True, $"{path} holds {text}");

    /// <summary>Whether a sheet is free of a piece of text, said as <see cref="Holds"/> says it.</summary>
    private static void Lacks(string path, string text) =>
        Assert.That(Sheet(path).Contains(text, StringComparison.Ordinal), Is.False, $"{path} has no {text}");

    /// <summary>
    /// The body of one of the surface tool's mixins: what stands between the brace after its name and the brace that
    /// closes it.
    /// </summary>
    private static string Mixin(string file, string name)
    {
        string source = Sheet($"tools/surface/{file}");
        int start = source.IndexOf($"@mixin {name}(", StringComparison.Ordinal);
        Assert.That(start, Is.GreaterThanOrEqualTo(0), $"@mixin {name} in {file}");

        int open = source.IndexOf('{', start);
        int depth = 0;
        for (int at = open; at < source.Length; at++)
        {
            depth += source[at] switch { '{' => 1, '}' => -1, _ => 0 };
            if (depth == 0) return source[(open + 1)..at];
        }

        return source[(open + 1)..];
    }

    /// <summary>
    /// What floats over the whole page, the search window, throws the shadow a theme gives what floats, and no shadow
    /// written as numbers.
    /// </summary>
    [Test]
    public void Hovering_ReadsTheThemesFloatingShadow()
    {
        string hovering = Mixin("_panes.scss", "hovering");

        Assert.Multiple(() =>
        {
            Assert.That(hovering, Does.Contain("box-shadow: var(--web-float-shadow);"));
            Assert.That(hovering, Does.Not.Contain("px"));
        });
    }

    /// <summary>The blur behind a piece is a theme's, the pane's unless a caller names another, never a number.</summary>
    [Test]
    public void Frosted_WritesNoBlurOfItsOwn()
    {
        string frosted = Mixin("_lines.scss", "frosted");

        Assert.Multiple(() =>
        {
            Assert.That(frosted, Does.Not.Contain("blur("));
            Holds("tools/surface/_lines.scss", "@mixin frosted($blur: var(--web-pane-blur))");
        });
    }

    /// <summary>The page behind the search window is blurred by the theme's veil, as what the stage shows through is.</summary>
    [Test]
    public void TheSearchWindowsBackdrop_TakesTheVeilsBlur() =>
        Holds("layout/_panels.scss", "@include surface.frosted(var(--web-veil-blur));");
}
