namespace LouisAgent.Core.Tests.Docs;

/// <summary>
/// A pack shows whoever runs it its numbers one way: as a described screen. The seam that came before stays gone, so
/// there's never a second way to do the same thing.
/// </summary>
public class OneSeamForScreensTests
{
    /// <summary>No code anywhere names the old seam. Its name is split so this file doesn't name it either.</summary>
    [Test]
    public void TheOldReportSeam_IsGone()
    {
        var oldSeam = "IData" + "Report";

        var named = new[] { "src", "packs", "demos" }
            .Select(folder => Path.Combine(TestPaths.RepoRoot, folder))
            .Where(Directory.Exists)
            .SelectMany(folder => Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories))
            .Where(IsSource)
            .Where(file => File.ReadAllText(file).Contains(oldSeam))
            .Select(file => Path.GetRelativePath(TestPaths.RepoRoot, file));

        Assert.That(named, Is.Empty);
    }

    /// <summary>Code we wrote: nothing the build made.</summary>
    private static bool IsSource(string file)
    {
        var inBuild = file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
            || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}");
        return !inBuild && Path.GetExtension(file) is ".cs" or ".razor";
    }
}
