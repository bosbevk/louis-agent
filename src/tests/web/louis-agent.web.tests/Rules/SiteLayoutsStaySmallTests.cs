namespace LouisAgent.Web.Tests.Rules;

/// <summary>
/// A site's layout only fills the framework's frame with what's its own, so it stays small. A ratchet, as
/// <see cref="RazorHasNoCodeTests"/> is: what's still too long is listed, a new one fails, and a listed one that has
/// shrunk fails too, so the list can only get shorter.
/// </summary>
[TestFixture]
public class SiteLayoutsStaySmallTests
{
    private const int MostLines = 40;

    /// <summary>Repo-relative, forward slashes; remove a layout's line in the commit that makes it small.</summary>
    private static readonly HashSet<string> Pending =
    [
    ];

    /// <summary>Every layout of a site on the framework stays under the limit, unless it's still pending.</summary>
    [Test]
    public void ASitesLayout_StaysSmall_UnlessStillPending()
    {
        var tooLong = FrameworkWebSites.Folders()
            .SelectMany(LayoutsOf)
            .Where(layout => File.ReadLines(layout).Count(line => line.Trim().Length > 0) > MostLines)
            .Select(layout => Path.GetRelativePath(TestPaths.RepoRoot, layout).Replace('\\', '/'))
            .ToHashSet();

        var newOffenders = tooLong.Except(Pending).Order().ToList();
        var done = Pending.Except(tooLong).Order().ToList();

        Assert.Multiple(() =>
        {
            Assert.That(newOffenders, Is.Empty,
                $"A site's layout holds at most {MostLines} lines; the frame belongs in louis-agent.web:\n  " +
                string.Join("\n  ", newOffenders));
            Assert.That(done, Is.Empty,
                "Small enough now: remove from Pending so the list only shrinks:\n  " + string.Join("\n  ", done));
        });
    }

    /// <summary>A site's layouts are the Razor files in its <c>Layout</c> folder.</summary>
    private static IEnumerable<string> LayoutsOf(string site)
    {
        string layouts = Path.Combine(site, "Layout");
        return Directory.Exists(layouts) ? Directory.EnumerateFiles(layouts, "*.razor") : [];
    }
}
