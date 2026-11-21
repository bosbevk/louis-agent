namespace LouisAgent.Web.Tests.Rules.Styles.Fonts;

/// <summary>
/// FX190: a <c>font</c> shorthand never mixes a size with <c>inherit</c>. Written <c>font: 500 13px/1.4 inherit</c>,
/// the browser throws the whole line away, so a press takes the browser's own button font, centred and, on a phone,
/// large enough to break over two lines, as the answer's choices and its way into the box did.
/// </summary>
public class FontShorthandTests
{
    /// <summary>The framework's web, its themes and its sites, where styles are written.</summary>
    private static readonly string[] Folders =
    [
        "src/web", "src/site", "demos"
    ];

    /// <summary>No style anywhere writes a size and <c>inherit</c> in one <c>font</c>.</summary>
    [Test]
    public void NoFontShorthand_MixesASizeWithInherit()
    {
        var broken = Folders
            .SelectMany(folder => Directory.EnumerateFiles(Path.Combine(TestPaths.RepoRoot, folder), "*.scss",
                SearchOption.AllDirectories))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .SelectMany(file => File.ReadLines(file).Select(line => (File: file, Line: line.Trim())))
            .Where(entry => entry.Line.StartsWith("font:", StringComparison.Ordinal))
            .Select(entry => (entry.File, Line: entry.Line["font:".Length..].Split(';')[0].Trim()))
            .Where(entry => entry.Line.Contains("inherit", StringComparison.Ordinal) && entry.Line != "inherit")
            .Select(entry => $"{Path.GetRelativePath(TestPaths.RepoRoot, entry.File)}: {entry.Line}");

        Assert.That(broken, Is.Empty);
    }
}
