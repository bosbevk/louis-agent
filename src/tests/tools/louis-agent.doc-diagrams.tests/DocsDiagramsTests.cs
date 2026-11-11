namespace LouisAgent.DocDiagrams.Tests;

/// <summary>
/// The docs' pictures stay what their JSON draws, and shown (CL60.1): every SVG in a <c>diagrams</c> folder is what
/// its JSON draws today, a doc beside the folder shows it, and every picture a doc shows is there.
/// </summary>
public class DocsDiagramsTests
{
    /// <summary>How to put a stale picture right, said when one is.</summary>
    private const string Fix =
        "run `dotnet run --project src/tools/louis-agent.doc-diagrams` and commit what it writes";

    /// <summary>
    /// Every picture is what its JSON draws, so a change to the drawing or the JSON can't leave it behind.
    /// </summary>
    [Test]
    public void EveryPicture_IsWhatItsJsonDraws()
    {
        var stale = Diagrams().Where(json => DiagramSync.Sync(json, check: true) != SyncOutcome.Unchanged);

        Assert.That(stale.Select(Relative), Is.Empty, Fix);
    }

    /// <summary>Every picture is shown by a doc in the folder that holds its <c>diagrams</c> folder.</summary>
    [Test]
    public void EveryPicture_IsShownByADocBesideIt()
    {
        var unshown = Diagrams().Select(json => Path.ChangeExtension(json, ".svg")).Where(svg =>
        {
            string folder = Path.GetDirectoryName(Path.GetDirectoryName(svg))!;
            string link = $"{DiagramSync.Folder}/{Path.GetFileName(svg)}";
            return !Directory.EnumerateFiles(folder, "*.md").Any(doc => Targets(File.ReadAllText(doc)).Contains(link));
        });

        Assert.That(unshown.Select(Relative), Is.Empty, "a picture no doc shows");
    }

    /// <summary>Every picture a doc shows from a <c>diagrams</c> folder is there.</summary>
    [Test]
    public void EveryPictureADocShows_IsThere()
    {
        var missing = DiagramSync.Roots(TestPaths.RepoRoot).Where(Directory.Exists)
            .SelectMany(root => Directory.EnumerateFiles(root, "*.md", SearchOption.AllDirectories))
            .SelectMany(doc => Targets(File.ReadAllText(doc))
                .Where(target => target.Contains($"{DiagramSync.Folder}/", StringComparison.Ordinal))
                .Where(target => !File.Exists(Path.Combine(Path.GetDirectoryName(doc)!, target)))
                .Select(target => $"{Relative(doc)} -> {target}"));

        Assert.That(missing, Is.Empty);
    }

    /// <summary>The docs have pictures at all, so the tests above can't pass by finding none.</summary>
    [Test]
    public void TheDocs_HavePictures() => Assert.That(Diagrams(), Is.Not.Empty);

    /// <summary>Every diagram's JSON file in the docs.</summary>
    private static IReadOnlyList<string> Diagrams() => DiagramSync.Find(DiagramSync.Roots(TestPaths.RepoRoot));

    /// <summary>
    /// The SVG pictures a doc shows: each markdown link's target, read between <c>](</c> and the <c>)</c> after it,
    /// outside code, since a how-to or a plan shows a link as an example there.
    /// </summary>
    private static IEnumerable<string> Targets(string doc)
    {
        bool fenced = false;
        foreach (string line in doc.ReplaceLineEndings("\n").Split('\n'))
        {
            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal)) fenced = !fenced;
            if (fenced) continue;

            // The parts between backticks are inline code: every other part, from the second.
            string prose = string.Concat(line.Split('`').Where((_, part) => part % 2 == 0));
            foreach (string target in LinkTargets(prose)
                         .Where(target => target.EndsWith(".svg", StringComparison.Ordinal)))
                yield return target;
        }
    }

    /// <summary>Each markdown link's target on a line of prose.</summary>
    private static IEnumerable<string> LinkTargets(string prose)
    {
        for (int at = prose.IndexOf("](", StringComparison.Ordinal);
             at >= 0;
             at = prose.IndexOf("](", at + 2, StringComparison.Ordinal))
        {
            int end = prose.IndexOf(')', at + 2);
            if (end < 0) yield break;
            yield return prose[(at + 2)..end];
        }
    }

    /// <summary>A path from the repo's root, with forward slashes, as a failure names it.</summary>
    private static string Relative(string path) => Path.GetRelativePath(TestPaths.RepoRoot, path).Replace('\\', '/');
}
