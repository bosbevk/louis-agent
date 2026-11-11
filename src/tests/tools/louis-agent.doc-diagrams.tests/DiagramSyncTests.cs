using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Svg;
using LouisAgent.DocDiagrams;

namespace LouisAgent.DocDiagrams.Tests;

/// <summary>
/// The docs' tool (CL60.1 step 6): every <c>diagrams/*.json</c> under the docs gets its SVG beside it, written only
/// when it differs, and a check run names a stale one without writing it, so the docs never show an old picture.
/// </summary>
public class DiagramSyncTests
{
    /// <summary>A small flow, as a doc's diagram file would hold it.</summary>
    private const string Flow = """
                                {
                                  "kind": "flow",
                                  "title": "Two boxes",
                                  "flow": {
                                    "nodes": [
                                      { "id": "a", "label": "A", "column": 0 },
                                      { "id": "b", "label": "B", "column": 1 }
                                    ],
                                    "edges": [ { "from": "a", "to": "b" } ]
                                  }
                                }
                                """;

    private string _docs = null!;
    private string _json = null!;

    /// <summary>A docs folder of its own, holding one diagram's file.</summary>
    [SetUp]
    public void SetUp()
    {
        _docs = Directory.CreateTempSubdirectory("docs-").FullName;
        Directory.CreateDirectory(Path.Combine(_docs, "projects", "diagrams"));
        _json = Path.Combine(_docs, "projects", "diagrams", "two.json");
        File.WriteAllText(_json, Flow);
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_docs, recursive: true);

    /// <summary>The SVG the renderer draws from the file, in the docs' frame.</summary>
    private static string Drawn() => DiagramRenderers.RenderTitled(DiagramReader.Read(Flow));

    /// <summary>Every diagram's file in a diagrams folder is found, and nothing else.</summary>
    [Test]
    public void TheFilesInADiagramsFolder_AreFound()
    {
        File.WriteAllText(Path.Combine(_docs, "projects", "not-a-diagram.json"), "{}");

        Assert.That(DiagramSync.Find([_docs]), Is.EqualTo(new[] { _json }));
    }

    /// <summary>A diagram without its SVG gets it, drawn from the file.</summary>
    [Test]
    public void ADiagramWithoutItsSvg_GetsIt()
    {
        var outcome = DiagramSync.Sync(_json, check: false);

        Assert.Multiple(() =>
        {
            Assert.That(outcome, Is.EqualTo(SyncOutcome.Written));
            Assert.That(File.ReadAllText(Path.ChangeExtension(_json, ".svg")), Is.EqualTo(Drawn()));
        });
    }

    /// <summary>A second run writes nothing, its line endings aside, so a checkout's own endings don't count.</summary>
    [Test]
    public void ASecondRun_WritesNothing()
    {
        File.WriteAllText(Path.ChangeExtension(_json, ".svg"), Drawn().ReplaceLineEndings("\r\n"));

        Assert.That(DiagramSync.Sync(_json, check: false), Is.EqualTo(SyncOutcome.Unchanged));
    }

    /// <summary>A check run names a stale SVG and leaves it as it is.</summary>
    [Test]
    public void ACheckRun_FindsAStaleSvg_AndLeavesIt()
    {
        string svg = Path.ChangeExtension(_json, ".svg");
        File.WriteAllText(svg, "<svg/>");

        var outcome = DiagramSync.Sync(_json, check: true);

        Assert.Multiple(() =>
        {
            Assert.That(outcome, Is.EqualTo(SyncOutcome.Stale));
            Assert.That(File.ReadAllText(svg), Is.EqualTo("<svg/>"));
        });
    }
}
