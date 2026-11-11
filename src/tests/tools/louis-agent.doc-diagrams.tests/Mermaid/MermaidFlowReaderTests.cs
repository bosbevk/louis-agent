using LouisAgent.Diagrams.Flow.Spec;
using LouisAgent.Diagrams.Sources;
using LouisAgent.DocDiagrams.Mermaid;

namespace LouisAgent.DocDiagrams.Tests.Mermaid;

/// <summary>
/// The one-time import of a docs flowchart (CL60.2): every form the docs write read into a flow, so the block can be
/// swapped for its picture and Mermaid kept nowhere.
/// </summary>
public class MermaidFlowReaderTests
{
    /// <summary>The flow a block reads into.</summary>
    private static GraphSpec Read(string block) => MermaidFlowReader.Read(block, "A chart").Flow!;

    /// <summary>The label of the box with this id.</summary>
    private static string Label(GraphSpec flow, string id) => flow.Nodes.Single(node => node.Id == id).Label;

    /// <summary>Each shape the docs use gives its words, a break as a new line and a tag left out.</summary>
    [Test]
    public void EveryShape_GivesItsWords()
    {
        var flow = Read("""
                        flowchart LR
                            a["louis-agent.core<br/>[class library]"] --> b(["Developer"])
                            c[("the ledger")] --> d{"a choice?"}
                            e(round) --> f[plain words] --> g[["<b>sub</b> routine"]]
                        """);

        Assert.Multiple(() =>
        {
            Assert.That(Label(flow, "a"), Is.EqualTo("louis-agent.core\n[class library]"));
            Assert.That(Label(flow, "b"), Is.EqualTo("Developer"));
            Assert.That(Label(flow, "c"), Is.EqualTo("the ledger"));
            Assert.That(Label(flow, "d"), Is.EqualTo("a choice?"));
            Assert.That(Label(flow, "e"), Is.EqualTo("round"));
            Assert.That(Label(flow, "f"), Is.EqualTo("plain words"));
            Assert.That(Label(flow, "g"), Is.EqualTo("sub routine"));
        });
    }

    /// <summary>Left to right runs right; top down, either way it's written, runs down.</summary>
    [TestCase("flowchart LR", FlowDirection.Right)]
    [TestCase("flowchart TB", FlowDirection.Down)]
    [TestCase("flowchart TD", FlowDirection.Down)]
    [TestCase("graph TD", FlowDirection.Down)]
    public void TheDirection_IsRead(string first, FlowDirection direction) =>
        Assert.That(Read($"{first}\n    a --> b").Direction, Is.EqualTo(direction));

    /// <summary>Each arrow form gives its word and whether it is dashed.</summary>
    [TestCase("a -->|calls| b", "calls", EdgeStyle.Solid)]
    [TestCase("a -- calls --> b", "calls", EdgeStyle.Solid)]
    [TestCase("a -.-> b", null, EdgeStyle.Dashed)]
    [TestCase("a -. maybe .-> b", "maybe", EdgeStyle.Dashed)]
    [TestCase("a --- b", null, EdgeStyle.Solid)]
    [TestCase("a -.->|later| b", "later", EdgeStyle.Dashed)]
    public void EveryArrowForm_GivesItsWordAndStyle(string line, string? word, EdgeStyle style)
    {
        var edge = Read($"flowchart LR\n    {line}").Edges.Single();

        Assert.That((edge.From, edge.To, edge.Word, edge.Style), Is.EqualTo(("a", "b", word, style)));
    }

    /// <summary>An arrow's word written in quotes reads without them.</summary>
    [Test]
    public void AQuotedWord_ReadsWithoutItsQuotes() =>
        Assert.That(Read("flowchart LR\n    a -->|\"fix sessions\"| b").Edges.Single().Word,
            Is.EqualTo("fix sessions"));

    /// <summary>
    /// A chart that runs right through more than five ranks is read as running down, so it fits a doc's column at
    /// full size; a shorter one keeps its way.
    /// </summary>
    [TestCase("a --> b --> c --> d --> e --> f --> g", FlowDirection.Down)]
    [TestCase("a --> b --> c --> d --> e", FlowDirection.Right)]
    public void AWideRightChart_RunsDown(string chain, FlowDirection direction) =>
        Assert.That(Read($"flowchart LR\n    {chain}").Direction, Is.EqualTo(direction));

    /// <summary>A chain is an arrow per link, and an ampersand an arrow from each.</summary>
    [Test]
    public void ChainsAndAmpersands_GiveAnArrowEach()
    {
        var flow = Read("""
                        flowchart LR
                            a --> b --> c
                            d & e --> f
                        """);

        Assert.That(flow.Edges.Select(edge => $"{edge.From}>{edge.To}"),
            Is.EqualTo(new[] { "a>b", "b>c", "d>f", "e>f" }));
    }

    /// <summary>
    /// A subgraph is a group with its label; a box stands in the innermost; an arrow to a subgraph goes to its first
    /// box; styles, classes, clicks and comments are left out.
    /// </summary>
    [Test]
    public void ASubgraph_IsAGroup()
    {
        var flow = Read("""
                        flowchart TB
                            dev(["Developer"])
                            %% a comment
                            subgraph sys["louis-agent [software system]"]
                                api["HTTP API"]
                                subgraph inner["the engine"]
                                    core["core"]
                                end
                            end
                            dev --> sys
                            classDef shaded fill:#eee
                            style api fill:#fff
                            click api "https://example.com"
                        """);

        Assert.Multiple(() =>
        {
            Assert.That(flow.Groups!.Select(group => (group.Id, group.Label)),
                Is.EqualTo(new[] { ("sys", "louis-agent [software system]"), ("inner", "the engine") }));
            Assert.That(flow.Nodes.Select(node => (node.Id, node.Group)),
                Is.EqualTo(new[] { ("dev", (string?)null), ("api", "sys"), ("core", "inner") }));
            Assert.That(flow.Edges.Single(), Is.EqualTo(new GraphEdge("dev", "api")));
        });
    }

    /// <summary>
    /// Importing a doc writes each flowchart as a JSON named for the doc and its heading, titled the same way, and
    /// says the link that takes the block's place.
    /// </summary>
    [Test]
    public void ImportingADoc_WritesEachFlowchart_AndItsLink()
    {
        string folder = Directory.CreateTempSubdirectory("louis-agent-import-").FullName;
        string doc = Path.Combine(folder, "louis-agent.core.md");
        File.WriteAllText(doc,
            "# louis-agent.core — design\n\n## How it works\n\n```mermaid\nflowchart LR\n    a --> b\n```\n");

        var imported = MermaidImport.Write(doc).Single();

        string json = Path.Combine(folder, "diagrams", "louis-agent.core-how-it-works.json");
        Assert.Multiple(() =>
        {
            Assert.That(imported.Json, Is.EqualTo(json));
            Assert.That(DiagramReader.Read(File.ReadAllText(json)).Title, Is.EqualTo("louis-agent.core: How it works"));
            Assert.That(imported.Link,
                Is.EqualTo("![louis-agent.core: How it works](diagrams/louis-agent.core-how-it-works.svg)"));
            Assert.That((imported.Block.Start, imported.Block.End), Is.EqualTo((5, 8)));
        });
    }

    /// <summary>A doc's flowcharts are found with their lines and the heading above; other kinds are left.</summary>
    [Test]
    public void ADocsFlowcharts_AreFound_WithTheirHeading()
    {
        const string doc = """
                           # louis-agent.core — design

                           ## How it works

                           ```mermaid
                           flowchart LR
                               a --> b
                           ```

                           ## Talking

                           ```mermaid
                           sequenceDiagram
                               a->>b: hi
                           ```
                           """;

        var block = MermaidImport.Blocks(doc).Single();

        Assert.That((block.Start, block.End, block.Heading), Is.EqualTo((5, 8, "How it works")));
        Assert.That(block.Body, Does.StartWith("flowchart LR"));
    }
}
