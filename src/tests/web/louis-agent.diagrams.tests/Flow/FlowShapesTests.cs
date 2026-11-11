using System.Xml.Linq;
using LouisAgent.Diagrams.Flow;
using LouisAgent.Diagrams.Flow.Spec;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Svg;

namespace LouisAgent.Diagrams.Tests.Flow;

/// <summary>
/// What the docs' flowcharts ask of a flow that the site's diagrams never did (CL60.2): a label over lines, a chart
/// that runs down, dashed arrows and groups.
/// </summary>
public class FlowShapesTests
{
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

    /// <summary>A label of two lines, the second long, and a box with one short line.</summary>
    private static GraphSpec TwoLines() => new(
        [
            new GraphNode("core", "louis-agent.core\n[class library: the engine, tools, skills]"),
            new GraphNode("host", "A host")
        ],
        [new GraphEdge("host", "core")]);

    /// <summary>A label's lines each fit its box, which is as wide as its longest line and a line taller.</summary>
    [Test]
    public void ALabelsLines_EachFitItsBox()
    {
        var core = GraphLayout.Place(TwoLines()).Nodes.Single(node => node.Node.Id == "core");

        Assert.Multiple(() =>
        {
            Assert.That(core.W, Is.GreaterThanOrEqualTo(
                "[class library: the engine, tools, skills]".Length * GraphLayout.LabelCharWidth));
            Assert.That(core.H, Is.GreaterThan(GraphLayout.Place(new GraphSpec([new GraphNode("a", "A")], []))
                .Nodes[0].H));
        });
    }

    /// <summary>A one-line label keeps the box the site's diagrams have always had.</summary>
    [Test]
    public void AOneLineLabel_KeepsTheBox()
    {
        var host = GraphLayout.Place(TwoLines()).Nodes.Single(node => node.Node.Id == "host");

        Assert.That((host.W, host.H), Is.EqualTo((170d, 52d)));
    }

    /// <summary>A chart that runs down: a host, the engine it starts, and the two things the engine calls.</summary>
    private static GraphSpec Down() => new(
        [
            new GraphNode("host", "A host"),
            new GraphNode("engine", "The engine\nAgentEngine"),
            new GraphNode("model", "The model"),
            new GraphNode("tools", "The tools")
        ],
        [new GraphEdge("host", "engine", "starts"), new GraphEdge("engine", "model"), new GraphEdge("engine", "tools")],
        FlowDirection.Down);

    /// <summary>A chart that runs down, drawn, matches its golden file.</summary>
    [Test]
    public void ADownChart_MatchesTheGoldenFile() =>
        Golden.Match("down.svg",
            DiagramRenderers.Render(new DiagramSource(DiagramKind.Flow, "A turn, down", Flow: Down())));

    /// <summary>Down, each rank is a row under the one before, and a rank's boxes stand side by side.</summary>
    [Test]
    public void ADownChart_PutsItsRanksInRows()
    {
        var at = GraphLayout.Place(Down()).Nodes.ToDictionary(node => node.Node.Id);

        Assert.Multiple(() =>
        {
            Assert.That(at["engine"].Y, Is.GreaterThan(at["host"].Y + at["host"].H));
            Assert.That(at["model"].Y, Is.GreaterThan(at["engine"].Y + at["engine"].H));
            Assert.That(at["model"].Y, Is.EqualTo(at["tools"].Y));
            Assert.That(at["tools"].X, Is.Not.EqualTo(at["model"].X));
        });
    }

    /// <summary>Down, a box keeps its own size, and an arrow leaves a box's bottom middle for the next row.</summary>
    [Test]
    public void ADownChart_KeepsItsBoxes_AndItsArrowsRunDown()
    {
        var placed = GraphLayout.Place(Down());
        var host = placed.Nodes.Single(node => node.Node.Id == "host");
        var engine = placed.Nodes.Single(node => node.Node.Id == "engine");

        var starts = placed.Edges.Single(edge => edge.Edge.To == "engine");
        Assert.Multiple(() =>
        {
            Assert.That((host.W, host.H), Is.EqualTo((170d, 52d)));
            Assert.That(starts.Path, Does.StartWith($"M {host.X + host.W / 2:0.#} {host.Y + host.H:0.#} "));
            Assert.That(starts.Path, Does.EndWith($"{engine.X + engine.W / 2:0.#} {engine.Y:0.#}"));
        });
    }

    /// <summary>
    /// Down, a word stands across its arrow, so a long word takes a line's height between two rows, not its length.
    /// </summary>
    [Test]
    public void ADownChart_KeepsItsRowsClose_ThoughAWordIsLong()
    {
        const string word = "Plugins.FromEnvironment() and a good deal more";
        var spec = new GraphSpec([new GraphNode("a", "A"), new GraphNode("b", "B")], [new GraphEdge("a", "b", word)],
            FlowDirection.Down);

        var at = GraphLayout.Place(spec).Nodes.ToDictionary(node => node.Node.Id);

        Assert.That(at["b"].Y - (at["a"].Y + at["a"].H), Is.LessThan(word.Length * 8));
    }

    /// <summary>
    /// Down, an arrow with a long word still bends inside its gap, so no arrow leaves the drawing (the accounts chart
    /// ran one up off its top).
    /// </summary>
    [Test]
    public void ADownChart_KeepsEveryArrowInsideTheDrawing()
    {
        var spec = new GraphSpec(
            [
                new GraphNode("browser", "Browser app"), new GraphNode("identity", "Identity's routes"),
                new GraphNode("ours", "AccountHandlers")
            ],
            [
                new GraphEdge("browser", "identity", "POST /account/login?useCookies=true"),
                new GraphEdge("browser", "ours", "GET /account/me, PUT /profile, POST /logout, DELETE /account")
            ],
            FlowDirection.Down);

        var placed = GraphLayout.Place(spec);

        var numbers = placed.Edges.SelectMany(edge => edge.Path.Split(' '))
            .Where(part => double.TryParse(part, out _)).Select(double.Parse);
        Assert.That(numbers, Has.All.InRange(0d, Math.Max(placed.Width, placed.Height)));
    }

    /// <summary>
    /// An arrow past a rank whose box stands in its way goes round, along a lane under the drawing, and crosses no
    /// box; an arrow with a clear way keeps it.
    /// </summary>
    [Test]
    public void AnArrowWithABoxInItsWay_GoesRound()
    {
        var spec = new GraphSpec(
            [new GraphNode("a", "A"), new GraphNode("b", "B"), new GraphNode("c", "C")],
            [new GraphEdge("a", "b"), new GraphEdge("b", "c"), new GraphEdge("a", "c", "past b")]);

        var placed = GraphLayout.Place(spec);

        var b = placed.Nodes.Single(node => node.Node.Id == "b");
        var past = placed.Edges.Single(edge => edge.Edge.Word == "past b");
        var ys = past.Path.Split(' ').Where(part => double.TryParse(part, out _)).Select(double.Parse)
            .Where((_, at) => at % 2 == 1).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(ys.Max(), Is.GreaterThan(b.Y + b.H), "the lane runs under the box in its way");
            Assert.That(past.WordY, Is.GreaterThan(b.Y + b.H), "its word sits on the lane");
            Assert.That(placed.Edges.Single(edge => edge.Edge.To == "b").Path, Does.Not.Contain("Q"),
                "a clear arrow runs straight");
        });
    }

    /// <summary>Down, an arrow's word starts just right of its line, and the drawing holds it.</summary>
    [Test]
    public void ADownChart_PutsAWordBesideItsLine()
    {
        const string word = "a rather long word on the arrow";
        var spec = new GraphSpec([new GraphNode("a", "A"), new GraphNode("b", "B")], [new GraphEdge("a", "b", word)],
            FlowDirection.Down);

        var placed = GraphLayout.Place(spec);

        var a = placed.Nodes.Single(node => node.Node.Id == "a");
        var edge = placed.Edges.Single();
        double left = edge.WordX - word.Length * FlowText.WordChar / 2;
        Assert.Multiple(() =>
        {
            Assert.That(left, Is.GreaterThan(a.X + a.W / 2), "it starts right of the line");
            Assert.That(edge.WordX + word.Length * FlowText.WordChar / 2, Is.LessThanOrEqualTo(placed.Width));
        });
    }

    /// <summary>The SVG draws each of a label's lines as its own words, inside the box.</summary>
    [Test]
    public void EachLine_IsDrawnInsideTheBox()
    {
        var placed = GraphLayout.Place(TwoLines());
        var core = placed.Nodes.Single(node => node.Node.Id == "core");
        var svg = XDocument.Parse(DiagramRenderers.Render(
            new DiagramSource(DiagramKind.Flow, "Two lines", Flow: TwoLines())));

        var lines = svg.Descendants(Svg + "text").Where(text => (string?)text.Attribute("class") == "label")
            .Where(text => text.Value != "A host").ToList();
        Assert.Multiple(() =>
        {
            Assert.That(lines.Select(line => line.Value),
                Is.EqualTo(new[] { "louis-agent.core", "[class library: the engine, tools, skills]" }));
            foreach (var line in lines)
                Assert.That(double.Parse((string)line.Attribute("y")!), Is.InRange(core.Y, core.Y + core.H));
        });
    }
}
