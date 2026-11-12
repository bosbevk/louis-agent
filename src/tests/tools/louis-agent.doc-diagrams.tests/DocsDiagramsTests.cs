using LouisAgent.Diagrams.Classes;
using LouisAgent.Diagrams.Flow;
using LouisAgent.Diagrams.Sequence;
using LouisAgent.Diagrams.Sequence.Placed;
using LouisAgent.Diagrams.Sequence.Spec.Steps;
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
        var bare = ProjectDocs().Where(doc => !Targets(File.ReadAllText(doc))
            .Contains($"{DiagramSync.Folder}/{Path.GetFileNameWithoutExtension(doc)}.svg"));

        Assert.That(bare.Select(Relative), Is.Empty, "a project doc without its overview");
    }

    /// <summary>
    /// Every project doc shows all four kinds of picture (CL60.5, Louis: "so each project has a
    /// architecture/flow/seq/interface diagrams"): its overview, a flow, a sequence and its types. A doc still to draw
    /// is pending, and a pending doc that has all four fails too.
    /// </summary>
    /// <remarks>
    /// A ratchet, as <c>RazorHasNoCodeTests</c> holds its pending code (CL60.5, Claude's pick Q3): the rule lands a
    /// few docs a step, each step taking its docs off the list, and a doc that's done can't slip back.
    /// </remarks>
    [Test]
    public void EveryProjectDoc_ShowsItsFourDiagrams()
    {
        var lacking = ProjectDocs().Where(doc => !Enum.GetValues<DiagramKind>().All(KindsShown(doc).Contains))
            .Select(Relative).ToHashSet();

        Assert.Multiple(() =>
        {
            Assert.That(lacking.Except(PendingFour).Order(), Is.Empty,
                "a project doc without all four kinds of picture");
            Assert.That(PendingFour.Except(lacking).Order(), Is.Empty,
                "a pending doc has all four: take it off the list");
        });
    }

    /// <summary>The project docs still to get their four pictures; a step removes the ones it draws.</summary>
    private static readonly HashSet<string> PendingFour =
    [
        // The web.
        "src/docs/projects/louis-agent.diagrams.md",
        "src/docs/projects/louis-agent.identity.guide.md",
        "src/docs/projects/louis-agent.identity.showroom.md",
        "src/docs/projects/louis-agent.site.md",
        "src/docs/projects/louis-agent.web.md",
        "src/docs/projects/louis-agent.web.accounts.md",

        // The packs.
        "packs/docs/projects/general-packs.md",
        "packs/docs/projects/louis-agent.pack.calculator.md",
        "packs/docs/projects/louis-agent.pack.docs.md",
        "packs/docs/projects/louis-agent.pack.leads.md",
        "packs/docs/projects/louis-agent.pack.recipes.md",
        "packs/docs/projects/louis-agent.pack.shop-prices.md",
        "packs/docs/projects/louis-agent.pack.site.md",
        "packs/docs/projects/louis-agent.pack.triage.md",
        "packs/docs/projects/louis-agent.pack.where-to-buy.md",

        // The demos.
        "demos/louis-agent/docs/projects/louis-agent.code-review.md",
        "demos/louis-agent/docs/projects/louis-agent.identity.triage.md",
        "demos/louis-agent/docs/projects/louis-agent.orchestrator.md",
        "demos/louis-agent/docs/projects/orchestrator-runbooks.md",
        "demos/puls/docs/projects/louis-agent.host-packs.md",
        "demos/puls/docs/projects/louis-agent.identity.puls.judges.md",
        "demos/puls/docs/projects/louis-agent.identity.puls.md",
        "demos/puls/docs/projects/louis-agent.puls.md",
    ];

    /// <summary>The kinds of picture a doc shows, read from each picture's JSON.</summary>
    private static IReadOnlySet<DiagramKind> KindsShown(string doc) => Targets(File.ReadAllText(doc))
        .Where(target => target.Contains($"{DiagramSync.Folder}/", StringComparison.Ordinal))
        .Select(target => Path.ChangeExtension(Path.Combine(Path.GetDirectoryName(doc)!, target), ".json"))
        .Where(File.Exists)
        .Select(json => DiagramReader.Read(File.ReadAllText(json)).Kind)
        .ToHashSet();

    /// <summary>
    /// Every project doc, the framework's, the packs' and each demo's: a demo's root is the whole demo, so its
    /// <c>projects</c> folder sits deeper, in its <c>docs</c>.
    /// </summary>
    private static IEnumerable<string> ProjectDocs() => DiagramSync.Roots(TestPaths.RepoRoot)
        .Where(Directory.Exists)
        .SelectMany(root => Directory.EnumerateDirectories(root, "projects", SearchOption.AllDirectories))
        .SelectMany(folder => Directory.EnumerateFiles(folder, "*.md"))
        .Where(doc => Path.GetFileName(doc) != "README.md");

    /// <summary>
    /// In every flow picture, a class chart's too (CL60.4, drawn as a flow), no word leaves the drawing or sits on a
    /// box, and no line crosses a box it doesn't join.
    /// </summary>
    [Test]
    public void EveryFlow_KeepsItsWordsAndLinesClear()
    {
        var faults = Diagrams().Select(json => (json, source: DiagramReader.Read(File.ReadAllText(json))))
            .Select(pair => (pair.json,
                flow: pair.source.Flow ?? (pair.source.Classes is { } classes ? ClassFlow.Of(classes) : null)))
            .Where(pair => pair.flow is not null)
            .SelectMany(pair => Clearance.Faults(GraphLayout.Place(pair.flow!, bend: GraphLayout.SoftBend))
                .Select(fault => $"{Path.GetFileNameWithoutExtension(pair.json)}: {fault}")).ToList();

        // The whole list, one fault a line: a list cut short hides the picture still to fix.
        Assert.That(faults, Is.Empty, string.Join('\n', faults));
    }

    /// <summary>
    /// In every sequence picture, no message's words, no note and no block's frame reaches past the drawing's edges
    /// (CL60.3, Claude's pick Q4: the same guard the flows have).
    /// </summary>
    [Test]
    public void EverySequence_KeepsItsWordsInsideTheDrawing()
    {
        var faults = Diagrams().Select(json => (json, source: DiagramReader.Read(File.ReadAllText(json))))
            .Where(pair => pair.source.Sequence is not null)
            .SelectMany(pair => Outside(SequenceLayout.Place(pair.source.Sequence!))
                .Select(what => $"{Path.GetFileNameWithoutExtension(pair.json)}: {what} leaves the drawing"))
            .ToList();

        Assert.That(faults, Is.Empty, string.Join('\n', faults));
    }

    /// <summary>What in a laid-out sequence reaches left of its start or right of its width.</summary>
    private static IEnumerable<string> Outside(PlacedSequence placed)
    {
        var boxes = placed.Rows
            .Select(row => (What: row.Lines.FirstOrDefault() ?? "",
                Box: row.Step is NoteStep ? SequenceText.NoteBox(row) : SequenceText.WordBox(row)))
            .Concat(placed.Blocks.Select(block =>
                (What: block.Block.Label, Box: (block.X, block.Y, block.W, block.H))));
        return boxes.Where(item => item.Box.Item1 < 0 || item.Box.Item1 + item.Box.Item3 > placed.Width + 0.001)
            .Select(item => $"'{item.What}'");
    }

    /// <summary>
    /// No doc holds Mermaid of any kind (CL60.4, the rule, Claude's pick Q4): every diagram is a JSON our renderer
    /// draws, so a new one goes the same way instead of slipping back in as a block GitHub draws its own way.
    /// </summary>
    [Test]
    public void NoDoc_HoldsMermaid()
    {
        var holding = Docs().Where(doc => MermaidStarts(File.ReadAllText(doc)).Any());

        Assert.That(holding.Select(Relative), Is.Empty, "a doc with a Mermaid block: import it as a picture");
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
