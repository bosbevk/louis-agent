namespace LouisAgent.Web.Tests.Rules.Styles.Themes;

/// <summary>
/// F169.4: a page's ground is exactly the colour its theme says. The framework lays nothing over it, such as the
/// faint grain it once spread over every page of every site (Louis's pick "A: take it out everywhere", 2026-11-22),
/// so a flat page is one solid colour and a theme's ground is never shifted by a layer the theme did not ask for.
/// </summary>
public class ThePagesGroundTests
{
    /// <summary>The framework's web styles, every sheet a site forwards from it.</summary>
    private static readonly string Styles = Path.Combine(TestPaths.RepoRoot, "src/web/LouisAgent.Web/Styles/web");

    /// <summary>The layer over the page that the grain was drawn on.</summary>
    private const string Layer = "body::before {";

    /// <summary>
    /// No sheet of the framework's web gives the layer over the page a picture or a colour, so nothing lies between a
    /// visitor and the theme's ground. A theme's own sheet may still draw there, as retro draws its scanlines.
    /// </summary>
    [Test]
    public void NoSheet_LaysAPictureOverThePage()
    {
        var drawn = Directory.EnumerateFiles(Styles, "*.scss", SearchOption.AllDirectories)
            .Where(file => Bodies(File.ReadAllText(file))
                .Any(body => body.Contains("background", StringComparison.Ordinal)))
            .Select(file => Path.GetRelativePath(Styles, file));

        Assert.That(drawn, Is.Empty);
    }

    /// <summary>The body of each rule a sheet writes for the layer over the page.</summary>
    private static IEnumerable<string> Bodies(string sheet)
    {
        for (int at = sheet.IndexOf(Layer, StringComparison.Ordinal);
             at >= 0;
             at = sheet.IndexOf(Layer, at + 1, StringComparison.Ordinal))
        {
            int open = at + Layer.Length;
            yield return sheet[open..sheet.IndexOf('}', open)];
        }
    }
}
