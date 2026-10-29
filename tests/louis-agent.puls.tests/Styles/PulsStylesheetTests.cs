namespace LouisAgent.Puls.Tests.Styles;

/// <summary>
/// Puls's stylesheet is app.css's import list (CL26): every area file is imported once, nothing imported is missing,
/// and the tokens come first so every later rule can use them.
/// </summary>
public class PulsStylesheetTests
{
    /// <summary>Puls's stylesheet.</summary>
    private static readonly Stylesheet Css = Stylesheet.Of("louis-agent.puls");

    /// <summary>Every area file is imported, and every import points at a file that's there.</summary>
    [Test]
    public void EveryAreaFile_IsImported_AndEveryImportIsThere() =>
        Assert.That(Css.Imports().Order(), Is.EqualTo(Css.AreaFiles().Order()));

    /// <summary>The tokens are imported first, then the plain elements: the rest of the files build on them.</summary>
    [Test]
    public void TheTokensComeFirst_ThenThePlainElements() =>
        Assert.That(Css.Imports().Take(2), Is.EqualTo(new[] { "settings/tokens.css", "base/elements.css" }));

    /// <summary>The stylesheet reads as the files in their import order, one after the other.</summary>
    [Test]
    public void TheStylesheet_IsTheFilesInImportOrder()
    {
        var starts = Css.Starts();

        Assert.That(starts, Is.All.GreaterThanOrEqualTo(0), "every file is in it");
        Assert.That(starts, Is.Ordered, "in import order");
    }
}
