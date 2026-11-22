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

        return BodyFrom(source, source.IndexOf('{', start));
    }

    /// <summary>
    /// Every rule a sheet of the framework's web writes for a selector, the tool and the settings left out: each
    /// rule's body, from the brace after the selector to the brace that closes it.
    /// </summary>
    private static List<string> RulesFor(string selector)
    {
        var bodies = new List<string>();
        var sheets = Directory.EnumerateFiles(Styles, "*.scss", SearchOption.AllDirectories)
            .Where(file => !file.Contains("/tools/", StringComparison.Ordinal) &&
                           !file.Contains("/settings/", StringComparison.Ordinal));
        foreach (string source in sheets.Select(File.ReadAllText))
        {
            // A rule starts a line, and its selector is followed by the brace that opens it.
            for (int at = source.IndexOf($"\n{selector} {{", StringComparison.Ordinal);
                 at >= 0;
                 at = source.IndexOf($"\n{selector} {{", at + 1, StringComparison.Ordinal))
                bodies.Add(BodyFrom(source, source.IndexOf('{', at)));
        }

        return bodies;
    }

    /// <summary>What stands between an opening brace and the brace that closes it.</summary>
    private static string BodyFrom(string source, int open)
    {
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

    /// <summary>
    /// The blur behind a piece is a theme's, the pane's unless a caller names another, never a number.
    /// </summary>
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

    /// <summary>
    /// The page behind the search window is blurred by the theme's veil, as what the stage shows through is.
    /// </summary>
    [Test]
    public void TheSearchWindowsBackdrop_TakesTheVeilsBlur() =>
        Holds("layout/_panels.scss", "@include surface.frosted(var(--web-veil-blur));");

    /// <summary>
    /// The row a chat is typed in floats as the search window does, by the theme's floating shadow, and no sheet
    /// gives it a deeper shadow of its own.
    /// </summary>
    [Test]
    public void TheTypingRow_Floats()
    {
        var rules = RulesFor(".input-row");

        Assert.Multiple(() =>
        {
            Assert.That(rules.Any(rule => rule.Contains("@include surface.hovering();", StringComparison.Ordinal)),
                Is.True, "a rule for .input-row includes surface.hovering()");
            Assert.That(rules.Where(rule => rule.Contains("surface.raised(", StringComparison.Ordinal) ||
                                            rule.Contains("surface.depth(", StringComparison.Ordinal)), Is.Empty);
        });
    }

    /// <summary>
    /// While it is typed in, the row keeps its pale ring and throws the theme's floating shadow beside it, and no
    /// shadow written as numbers; the chat's other box lights by the same mixin, so the ring is written once.
    /// </summary>
    [Test]
    public void TypedIn_IsARingBesideTheFloatingShadow()
    {
        string typedIn = Mixin("_talk.scss", "typed-in");

        Assert.Multiple(() =>
        {
            Assert.That(typedIn, Does.Contain("var(--web-float-shadow);"));
            Assert.That(typedIn, Does.Not.Contain("40px"));
            Assert.That(typedIn.Split("var(--web-shadow-rgb)").Length, Is.EqualTo(1), "no shadow of its own");
            Assert.That(Mixin("_panes.scss", "lit"), Does.Contain("@include typed-in();"));
        });
    }

    /// <summary>
    /// The send throws the press's shadow, the same at rest and under the pointer, and stands still: no glow of its
    /// own, and no breathing.
    /// </summary>
    [Test]
    public void TheSend_ThrowsThePresssShadow_AndStandsStill()
    {
        var send = RulesFor(".send");
        var hovered = RulesFor(".send:not(:disabled):hover");
        bool breathes = Directory.EnumerateFiles(Styles, "*.scss", SearchOption.AllDirectories)
            .Any(file => File.ReadAllText(file).Contains("@keyframes breathe", StringComparison.Ordinal));

        Assert.Multiple(() =>
        {
            Assert.That(send.Any(rule => rule.Contains("@include surface.press-shadow();", StringComparison.Ordinal)),
                Is.True, "the send includes surface.press-shadow()");
            Assert.That(send.Where(rule => rule.Contains("surface.cast(", StringComparison.Ordinal)), Is.Empty);
            Assert.That(hovered.Where(rule => rule.Contains("shadow", StringComparison.Ordinal) ||
                                              rule.Contains("surface.cast(", StringComparison.Ordinal)), Is.Empty);
            Assert.That(breathes, Is.False, "no sheet holds a breathe animation");
            Assert.That(Mixin("_presses.scss", "press-shadow"), Does.Contain("box-shadow: var(--web-press-shadow);"));
        });
    }

    /// <summary>
    /// The pieces of a talk take the theme's small shadow: your message by the bubble's own mixin and a code block by
    /// a raised piece's, and no sheet of the framework's web lays a deeper shadow of its own on any of them.
    /// </summary>
    [Test]
    public void TheTalksPieces_TakeTheThemesSmallShadow()
    {
        var sheets = Directory.EnumerateFiles(Styles, "*.scss", SearchOption.AllDirectories)
            .Where(file => !file.Contains("/tools/", StringComparison.Ordinal))
            .Select(File.ReadAllText).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(Mixin("_talk.scss", "spoken"), Does.Contain("box-shadow: var(--bs-box-shadow-sm);"));
            Assert.That(RulesFor(".markdown pre").Any(rule =>
                rule.Contains("@include surface.raised();", StringComparison.Ordinal)), Is.True,
                "a code block's rule includes surface.raised()");
            Assert.That(sheets.Count(sheet => sheet.Contains("surface.depth(", StringComparison.Ordinal)), Is.Zero,
                "no sheet includes surface.depth(");
            Assert.That(sheets.Count(sheet => sheet.Contains("surface.spoken-deep(", StringComparison.Ordinal)),
                Is.Zero, "no sheet includes surface.spoken-deep(");
        });
    }

    /// <summary>
    /// The line that says the agent is working runs from the words' grey to the theme's working light and back, and
    /// names no other colour, so a theme that sets the light to the words' own grey shows plain words (Louis's pick
    /// "A: plain words in flat", 2026-11-22).
    /// </summary>
    [Test]
    public void TheWorkingLine_RunsToTheThemesWorkingLight()
    {
        string shimmer = Mixin("_talk.scss", "shimmer");

        Assert.Multiple(() =>
        {
            Assert.That(shimmer, Does.Contain("var(--bs-secondary-color) 0%, var(--web-working-light) 50%, " +
                                              "var(--bs-secondary-color) 100%"));
            Assert.That(shimmer.Split("var(--").Length, Is.EqualTo(4), "three names and no other colour");
        });
    }
}
