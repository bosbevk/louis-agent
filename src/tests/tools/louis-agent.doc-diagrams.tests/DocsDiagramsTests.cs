using LouisAgent.Diagrams.Flow;
using LouisAgent.Diagrams.Sources;

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

    /// <summary>
    /// Every picture is shown by a doc: one beside its <c>diagrams</c> folder, or one elsewhere that links it by its
    /// path, as the repo's README shows the framework's picture from <c>src/docs/diagrams</c> (CL60.2, so the top
    /// level keeps only <c>src</c>, <c>demos</c> and <c>packs</c>).
    /// </summary>
    [Test]
    public void EveryPicture_IsShownByADoc()
    {
        // Each picture link, as the full path of the file it points at.
        var shown = Docs().SelectMany(doc => Targets(File.ReadAllText(doc))
                .Where(target => target.Contains($"{DiagramSync.Folder}/", StringComparison.Ordinal))
                .Select(target => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(doc)!, target))))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var unshown = Diagrams().Select(json => Path.ChangeExtension(json, ".svg"))
            .Where(svg => !shown.Contains(Path.GetFullPath(svg)));
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

    /// <summary>
    /// Every project doc, the framework's, the packs' and each demo's, shows its overview, named for the doc.
    /// </summary>
    [Test]
    public void EveryProjectDoc_ShowsItsOverview()
    {
        var bare = DiagramSync.Roots(TestPaths.RepoRoot)
            .Select(root => Path.Combine(root, "projects"))
            .Where(Directory.Exists)
            .SelectMany(folder => Directory.EnumerateFiles(folder, "*.md"))
            .Where(doc => Path.GetFileName(doc) != "README.md")
            .Where(doc => !Targets(File.ReadAllText(doc))
                .Contains($"{DiagramSync.Folder}/{Path.GetFileNameWithoutExtension(doc)}.svg"));

        Assert.That(bare.Select(Relative), Is.Empty, "a project doc without its overview");
    }

    /// <summary>
    /// In every flow picture, no word leaves the drawing or sits on a box, and no line crosses a box it doesn't join.
    /// </summary>
    [Test]
    public void EveryFlow_KeepsItsWordsAndLinesClear()
    {
        var faults = Diagrams().Select(json => (json, source: DiagramReader.Read(File.ReadAllText(json))))
            .Where(pair => pair.source.Flow is not null)
            .SelectMany(pair => Clearance.Faults(GraphLayout.Place(pair.source.Flow!, bend: GraphLayout.SoftBend))
                .Select(fault => $"{Path.GetFileNameWithoutExtension(pair.json)}: {fault}")).ToList();

        // The whole list, one fault a line: a list cut short hides the picture still to fix.
        Assert.That(faults, Is.Empty, string.Join('\n', faults));
    }

    /// <summary>
    /// No doc draws a flow in Mermaid any more (CL60.2): every flow is a JSON our renderer draws, so a new one goes
    /// the same way instead of slipping back in as a block GitHub draws its own way.
    /// </summary>
    [Test]
    public void NoDoc_HoldsAMermaidFlowchart()
    {
        var holding = Docs().Where(doc => MermaidStarts(File.ReadAllText(doc))
            .Any(first => first.StartsWith("flowchart", StringComparison.Ordinal)
                          || first.StartsWith("graph", StringComparison.Ordinal)));

        Assert.That(holding.Select(Relative), Is.Empty, "a doc with a Mermaid flowchart: import it as a picture");
    }

    /// <summary>The docs have pictures at all, so the tests above can't pass by finding none.</summary>
    [Test]
    public void TheDocs_HavePictures() => Assert.That(Diagrams(), Is.Not.Empty);

    /// <summary>
    /// Every picture's JSON in the repo is one the tool draws and these tests check, wherever its doc sits: a
    /// demo's deploy notes keep their pictures beside them (CL60.2, the Azure architecture's two).
    /// </summary>
    [Test]
    public void EveryDiagramsFolder_IsOneTheToolDraws()
    {
        var everywhere = new[] { "src", "demos", "packs" }.Select(top => Path.Combine(TestPaths.RepoRoot, top))
            .SelectMany(top => Directory.EnumerateDirectories(top, DiagramSync.Folder, SearchOption.AllDirectories))
            .SelectMany(folder => Directory.EnumerateFiles(folder, "*.json"));

        Assert.That(everywhere.Except(Diagrams()).Select(Relative), Is.Empty, "a picture the tool never draws");
    }

    /// <summary>Every diagram's JSON file in the docs.</summary>
    private static IReadOnlyList<string> Diagrams() => DiagramSync.Find(DiagramSync.Roots(TestPaths.RepoRoot));

    /// <summary>Every doc the tool's roots hold, and the repo's README.</summary>
    private static IEnumerable<string> Docs() => DiagramSync.Roots(TestPaths.RepoRoot).Where(Directory.Exists)
        .SelectMany(root => Directory.EnumerateFiles(root, "*.md", SearchOption.AllDirectories))
        .Append(Path.Combine(TestPaths.RepoRoot, "README.md"));

    /// <summary>The first line of each Mermaid block in a doc, which names the kind of chart it draws.</summary>
    private static IEnumerable<string> MermaidStarts(string doc)
    {
        string[] lines = doc.ReplaceLineEndings("\n").Split('\n');
        for (int at = 0; at < lines.Length - 1; at++)
            if (lines[at].Trim() == "```mermaid")
                yield return lines[at + 1].Trim();
    }

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
