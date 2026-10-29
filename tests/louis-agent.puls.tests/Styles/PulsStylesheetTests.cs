namespace LouisAgent.Puls.Tests.Styles;

/// <summary>
/// Puls's stylesheet is app.css's import list (CL26): every area file is imported once, nothing imported is missing,
/// and the tokens come first so every later rule can use them.
/// </summary>
public class PulsStylesheetTests
{
    /// <summary>Every area file is imported, and every import points at a file that's there.</summary>
    [Test]
    public void EveryAreaFile_IsImported_AndEveryImportIsThere()
    {
        var onDisk = Directory.EnumerateFiles(PulsStylesheet.Folder, "*.css", SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(PulsStylesheet.Folder, file).Replace('\\', '/'))
            .Where(file => file != "app.css")
            .Order();

        Assert.That(PulsStylesheet.Imports().Order(), Is.EqualTo(onDisk));
    }

    /// <summary>The tokens are imported first, then the plain elements: the rest of the files build on them.</summary>
    [Test]
    public void TheTokensComeFirst_ThenThePlainElements()
    {
        Assert.That(PulsStylesheet.Imports().Take(2), Is.EqualTo(new[] { "settings/tokens.css", "base/elements.css" }));
    }

    /// <summary>The stylesheet reads as the files in their import order, one after the other.</summary>
    [Test]
    public void TheStylesheet_IsTheFilesInImportOrder()
    {
        string css = PulsStylesheet.Read();

        var starts = PulsStylesheet.Imports()
            .Select(file => File.ReadAllText(Path.Combine(PulsStylesheet.Folder, file)))
            .Select(text => css.IndexOf(text, StringComparison.Ordinal))
            .ToList();
        Assert.That(starts, Is.All.GreaterThanOrEqualTo(0));
        Assert.That(starts, Is.Ordered);
    }
}
