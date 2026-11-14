using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Classes;
using LouisAgent.Diagrams.Engine.Composite;
using LouisAgent.Diagrams.Engine.Flow;
using LouisAgent.Diagrams.Engine.Sequence;
using LouisAgent.Diagrams.Flow.Spec;
using LouisAgent.Diagrams.Sequence.Spec;
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
    /// architecture/flow/seq/interface diagrams"): its overview, a flow, a sequence and its types, so a new project's
    /// doc can't go without one.
    /// </summary>
    [Test]
    public void EveryProjectDoc_ShowsItsFourDiagrams()
    {
        var lacking = ProjectDocs().Where(doc => !ProjectDocKinds.All(KindsShown(doc).Contains));

        Assert.That(lacking.Select(Relative), Is.Empty, "a project doc without all four kinds of picture");
    }

    /// <summary>
    /// The four a project doc shows, named rather than every kind there is: a ticket (F127.4.1) is a card an agent
    /// shows in a chat, not a picture of a project.
    /// </summary>
    private static readonly DiagramKind[] ProjectDocKinds =
        [DiagramKind.Cards, DiagramKind.Flow, DiagramKind.Sequence, DiagramKind.Classes];

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
    /// The flow a picture is placed as: its own, a class chart's (CL60.4) or a composite's (F134.13), each through
    /// its adapter. Nothing for a kind that has a layout of its own.
    /// </summary>
    private static GraphSpec? FlowOf(DiagramSource source) => source.Kind switch
    {
        DiagramKind.Flow => source.Flow,
        DiagramKind.Classes => ClassFlow.Of(source.Classes!),
        DiagramKind.Composite => CompositeFlow.Of(source.Composite!),
        _ => null
    };

    /// <summary>
    /// A composite structure diagram is placed by the flow's layout (F134.13), so the docs hold it to what they
    /// hold a flow to: clear words and lines, and no two lines on one another. And the docs have one to hold.
    /// </summary>
    [Test]
    public void AComposite_IsHeldAsAFlow_AndTheDocsHaveOne()
    {
        var composites = Diagrams().Select(json => DiagramReader.Read(File.ReadAllText(json)))
            .Where(source => source.Kind == DiagramKind.Composite).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(composites, Is.Not.Empty, "the docs have a composite structure diagram");
            Assert.That(composites.Select(FlowOf), Has.None.Null, "each is laid out as a flow, and so held clear");
        });
    }

    /// <summary>
    /// In every flow picture, a class chart's too (CL60.4, drawn as a flow), no word leaves the drawing or sits on a
    /// box, and no line crosses a box it doesn't join. Each is laid out as it is drawn: straight and level
    /// (F134.8), and a class chart with its names wrapped and a badge's room beside each (F134.11).
    /// </summary>
    [Test]
    public void EveryFlow_KeepsItsWordsAndLinesClear()
    {
        var faults = Diagrams().Select(json => (json, source: DiagramReader.Read(File.ReadAllText(json))))
            .Select(pair => (pair.json, flow: FlowOf(pair.source)))
            .Where(pair => pair.flow is not null)
            .SelectMany(pair => Clearance
                .Faults(GraphLayout.Place(pair.flow!, bend: GraphLayout.SoftBend, rules: FlowRules.Straight))
                .Select(fault => $"{Path.GetFileNameWithoutExtension(pair.json)}: {fault}")).ToList();

        // The whole list, one fault a line: a list cut short hides the picture still to fix.
        Assert.That(faults, Is.Empty, string.Join('\n', faults));
    }

    /// <summary>
    /// Laid out by the rules a flow gets (F134.8), arrows apart and boxes level, no flow in the docs has two lines
    /// from different boxes lying on one another: each line can be followed from its box to where it arrives. A
    /// class chart is laid out the same way now (F134.11), so it is held to the same.
    /// </summary>
    [Test]
    public void EveryFlow_LaidOutApart_HasNoTwoLinesOnOneAnother()
    {
        var rules = FlowRules.Straight;
        var lying = Diagrams().Select(json => (json, source: DiagramReader.Read(File.ReadAllText(json))))
            .Select(pair => (pair.json, flow: FlowOf(pair.source)))
            .Where(pair => pair.flow is not null)
            .SelectMany(pair => Clearance.Lying(GraphLayout.Place(pair.flow!, bend: GraphLayout.SoftBend, rules: rules))
                .Select(lines => $"{Path.GetFileNameWithoutExtension(pair.json)}: {lines}")).ToList();

        // The whole list, one pair a line: a list cut short hides the picture still to fix.
        Assert.That(lying, Is.Empty, string.Join('\n', lying));
    }

    /// <summary>
    /// In every sequence picture, no message's words, no note and no block's frame reaches past the drawing's edges
    /// (CL60.3, Claude's pick Q4: the same guard the flows have), by the measures a sequence is drawn with
    /// (F134.10): a head with no box, words above their line and a frame that reaches left for its tab.
    /// </summary>
    [Test]
    public void EverySequence_KeepsItsWordsInsideTheDrawing()
    {
        var faults = Diagrams().Select(json => (json, source: DiagramReader.Read(File.ReadAllText(json))))
            .Where(pair => pair.source.Sequence is not null)
            .SelectMany(pair => Outside(pair.source.Sequence!, SequenceMeasures.Marquee)
                .Select(what => $"{Path.GetFileNameWithoutExtension(pair.json)}: {what} leaves the drawing"))
            .ToList();

        Assert.That(faults, Is.Empty, string.Join('\n', faults));
    }

    /// <summary>
    /// What in a sequence, laid out by the measures given, reaches left of its start or right of its width.
    /// </summary>
    private static IEnumerable<string> Outside(SequenceSpec sequence, SequenceMeasures measures)
    {
        var geometry = Geometries.InUse;
        var placed = SequenceLayout.Place(sequence, measures, geometry);
        var boxes = placed.Rows
            .Select(row => (What: row.Lines.FirstOrDefault() ?? "",
                Box: row.Step is NoteStep ? measures.NoteBox(row, geometry) : measures.WordBox(row, geometry)))
            .Concat(placed.Blocks.Select(block => (What: block.Block.Label, block.Box)));
        return boxes.Where(item => item.Box.Left < 0 || item.Box.Right > placed.Width + 0.001)
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
