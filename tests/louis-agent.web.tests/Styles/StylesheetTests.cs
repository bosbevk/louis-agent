namespace LouisAgent.Web.Tests.Styles;

/// <summary>
/// The code-review app's stylesheet is app.css's import list (CL26): every area file is imported, nothing imported is
/// missing, and the page's base comes first, with the variables a theme sets.
/// </summary>
public class StylesheetTests
{
    /// <summary>The code-review app's stylesheet.</summary>
    private static readonly Stylesheet Css = Stylesheet.Of("louis-agent.web");

    /// <summary>Every area file is imported, and every import points at a file that's there.</summary>
    [Test]
    public void EveryAreaFile_IsImported_AndEveryImportIsThere() =>
        Assert.That(Css.Imports().Order(), Is.EqualTo(Css.AreaFiles().Order()));

    /// <summary>The page's base is imported first: the rest of the files build on it.</summary>
    [Test]
    public void ThePagesBase_ComesFirst() => Assert.That(Css.Imports()[0], Is.EqualTo("base/page.css"));

    /// <summary>The stylesheet reads as the files in their import order, one after the other.</summary>
    [Test]
    public void TheStylesheet_IsTheFilesInImportOrder()
    {
        var starts = Css.Starts();

        Assert.That(starts, Is.All.GreaterThanOrEqualTo(0), "every file is in it");
        Assert.That(starts, Is.Ordered, "in import order");
    }
}
