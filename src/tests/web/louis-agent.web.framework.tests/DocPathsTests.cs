namespace LouisAgent.Web.Framework.Tests;

/// <summary>
/// F161.1: every section of the framework's pages hands off to its docs by their paths in the repo, so a doc that
/// moves or goes must take its links with it: a path that names no file fails here, before a visitor finds it.
/// </summary>
public class DocPathsTests
{
    /// <summary>The words a doc link's path follows in the pages' markup.</summary>
    private const string PathOpens = "<DocLink Path=\"";

    /// <summary>Every path a doc link on the framework's pages names, with the page it is on.</summary>
    private static IEnumerable<(string Page, string Path)> DocPaths()
    {
        string pages = Path.Combine(TestPaths.RepoRoot, "src/web/louis-agent.web.framework");
        foreach (string page in Directory.EnumerateFiles(pages, "*.razor", SearchOption.AllDirectories))
        {
            string markup = File.ReadAllText(page);
            for (int at = markup.IndexOf(PathOpens, StringComparison.Ordinal); at >= 0;
                 at = markup.IndexOf(PathOpens, at + 1, StringComparison.Ordinal))
            {
                int start = at + PathOpens.Length;
                yield return (System.IO.Path.GetFileName(page), markup[start..markup.IndexOf('"', start)]);
            }
        }
    }

    /// <summary>Every doc a page links to is a file in the repo.</summary>
    [Test]
    public void EveryDocLink_IsAFileInTheRepo()
    {
        var missing = DocPaths().Where(link => !File.Exists(System.IO.Path.Combine(TestPaths.RepoRoot, link.Path)))
            .Select(link => $"{link.Page}: {link.Path}");

        Assert.That(missing, Is.Empty);
    }
}
