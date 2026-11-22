namespace LouisAgent.Web.Framework.Tests;

/// <summary>
/// The framework's pages call an agent's helper what the engine calls it, a subagent (CL68.2, ADR 0133: "a
/// specialist is a subagent, in the code, the headers and the texts"), so no page record or page says the old word.
/// </summary>
public class SubagentWordTests
{
    /// <summary>The old word, in any case.</summary>
    private const string OldWord = "specialist";

    /// <summary>Every page record and every page of the framework's site, by its path in the repo.</summary>
    private static IEnumerable<string> PageFiles()
    {
        string pages = Path.Combine(TestPaths.RepoRoot, "src/web/LouisAgent.Web.Framework");
        return Directory.EnumerateFiles(pages, "*.*", SearchOption.AllDirectories)
            .Where(file => file.EndsWith(".razor", StringComparison.Ordinal) ||
                           file.EndsWith(".cs", StringComparison.Ordinal))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") &&
                           !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));
    }

    /// <summary>No page and no page record says specialist.</summary>
    [Test]
    public void NoFrameworkPage_SaysSpecialist()
    {
        var saying = PageFiles()
            .Where(file => File.ReadAllText(file).Contains(OldWord, StringComparison.OrdinalIgnoreCase))
            .Select(file => Path.GetRelativePath(TestPaths.RepoRoot, file).Replace('\\', '/'));

        Assert.That(saying, Is.Empty);
    }
}
