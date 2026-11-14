using System.Xml.Linq;
using LouisAgent.Diagrams.Engine.Flow;
using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Flow.Spec;
using LouisAgent.Diagrams.Tests.Flow.Arrows;

namespace LouisAgent.Diagrams.Tests.Flow;

/// <summary>
/// What the docs' flowcharts ask of a flow that the site's diagrams never did (CL60.2): a label over lines, a chart
/// that runs down, dashed arrows and groups.
/// </summary>
public class FlowShapesTests
{
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
            Assert.That(core.Box.Width, Is.GreaterThanOrEqualTo(
                GraphLayout.Label.Width("[class library: the engine, tools, skills]")));
            Assert.That(core.Box.Height, Is.GreaterThan(GraphLayout.Place(new GraphSpec([new GraphNode("a", "A")], []))
                .Nodes[0].Box.Height));
        });
    }

    /// <summary>A one-line label keeps the box the site's diagrams have always had.</summary>
    [Test]
    public void AOneLineLabel_KeepsTheBox()
    {
        var host = GraphLayout.Place(TwoLines()).Nodes.Single(node => node.Node.Id == "host");

        Assert.That((host.Box.Width, host.Box.Height), Is.EqualTo((170d, 52d)));
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

    /// <summary>Down, each rank is a row under the one before, and a rank's boxes stand side by side.</summary>
    [Test]
    public void ADownChart_PutsItsRanksInRows()
    {
        var at = GraphLayout.Place(Down()).Nodes.ToDictionary(node => node.Node.Id);

        Assert.Multiple(() =>
        {
            Assert.That(at["engine"].Box.Top, Is.GreaterThan(at["host"].Box.Bottom));
            Assert.That(at["model"].Box.Top, Is.GreaterThan(at["engine"].Box.Bottom));
            Assert.That(at["model"].Box.Top, Is.EqualTo(at["tools"].Box.Top));
            Assert.That(at["tools"].Box.Left, Is.Not.EqualTo(at["model"].Box.Left));
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
            Assert.That((host.Box.Width, host.Box.Height), Is.EqualTo((170d, 52d)));
            Assert.That(Ways.Points(starts.Way)[0], Is.EqualTo(Ways.At(host.Box.Middle.X, host.Box.Bottom)));
            Assert.That(Ways.Points(starts.Way)[^1], Is.EqualTo(Ways.At(engine.Box.Middle.X, engine.Box.Top)));
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

        Assert.That(at["b"].Box.Top - (at["a"].Box.Bottom), Is.LessThan(word.Length * 8));
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

        var numbers = placed.Edges.SelectMany(edge => Ways.Numbers(edge.Way));
        Assert.That(numbers, Has.All.InRange(0d, Math.Max(placed.Width, placed.Height)));
    }

    /// <summary>
    /// An arrow past a rank runs through that rank beside its boxes, as layered drawing's waypoints have it: no box
    /// in its way, no lane round the drawing, the waypoint drawn as nothing, and still one arrow with its word.
    /// </summary>
    [Test]
    public void AnArrowPastARank_RunsBesideItsBoxes()
    {
        var spec = new GraphSpec(
            [
                new GraphNode("host", "CliHost"), new GraphNode("chat", "ConsoleChat"),
                new GraphNode("engine", "Engine"),
                new GraphNode("out", "stdout")
            ],
            [
                new GraphEdge("host", "chat"), new GraphEdge("chat", "engine"), new GraphEdge("chat", "out"),
                new GraphEdge("host", "engine", "AgentHost.Build")
            ],
            FlowDirection.Down);

        var placed = GraphLayout.Place(spec);

        var past = placed.Edges.Single(edge => edge.Edge.Word == "AgentHost.Build");
        var numbers = Ways.Numbers(past.Way);
        var chat = placed.Nodes.Single(node => node.Node.Id == "chat");
        var geometry = Geometries.InUse;
        var through = past.Way.Stretches.Where(stretch => chat.Box.CrossedBy(
            geometry.Point(stretch.From.X, stretch.From.Y), geometry.Point(stretch.To.X, stretch.To.Y), 1));
        Assert.Multiple(() =>
        {
            Assert.That(placed.Nodes.Select(node => node.Node.Id),
                Is.EquivalentTo(new[] { "host", "chat", "engine", "out" }));
            Assert.That(placed.Edges.Count(edge => edge.Edge is { From: "host", To: "engine" }), Is.EqualTo(1));
            Assert.That(numbers.Where((_, at) => at % 2 == 1).Max(), Is.LessThan(placed.Height), "no lane under it");
            Assert.That(through, Is.Empty, "it passes beside the box, not through it");
        });
    }

    /// <summary>
    /// A long arrow drops straight into its target, its waypoints in line with it where no box is in the way (Louis,
    /// of the CLI chart: along, then straight down); and a target right under its source gets one straight line.
    /// </summary>
    [Test]
    public void ALongArrow_DropsStraightIntoItsTarget()
    {
        var spec = new GraphSpec(
            [
                new GraphNode("host", "CliHost"), new GraphNode("chat", "ConsoleChat"), new GraphNode("out", "stdout"),
                new GraphNode("err", "stderr"), new GraphNode("engine", "AgentEngine (core)")
            ],
            [
                new GraphEdge("host", "chat"), new GraphEdge("chat", "out"), new GraphEdge("chat", "err"),
                new GraphEdge("chat", "engine"), new GraphEdge("host", "engine", "AgentHost.Build")
            ],
            FlowDirection.Down);

        var placed = GraphLayout.Place(spec);

        var engine = placed.Nodes.Single(node => node.Node.Id == "engine");
        var numbers = Ways.Numbers(placed.Edges.Single(edge => edge.Edge.Word == "AgentHost.Build").Way);
        // Out of its source, along, and straight down into the target: its upright pieces stand at two places only,
        // the source's middle and the target's, with no jog through a slot between (a curve's corners left out).
        var host = placed.Nodes.Single(node => node.Node.Id == "host");
        var xs = numbers.Where((_, at) => at % 2 == 0).Select(x => Math.Round(x)).ToList();
        Assert.That(
            xs.Where(x => Math.Abs(x - host.Box.Middle.X) > 10 && Math.Abs(x - engine.Box.Middle.X) > 10),
            Is.Empty);
    }

    /// <summary>
    /// A long arrow whose target's line is blocked drops straight from its source instead, and turns at the end
    /// into its own point on its target's side (Louis, of the telemetry chart).
    /// </summary>
    [Test]
    public void ALongArrowBlockedAtItsTarget_DropsStraightFromItsSource()
    {
        var spec = new GraphSpec(
            [
                new GraphNode("build", "AgentHost.Build"), new GraphNode("logging", "AgentLogging"),
                new GraphNode("otlp", "OtlpTelemetry"), new GraphNode("chance", "LastChance, told what to flush"),
                new GraphNode("filter", "OwnTrafficFilter"), new GraphNode("collector", "the collector")
            ],
            [
                new GraphEdge("build", "logging"), new GraphEdge("build", "otlp"), new GraphEdge("otlp", "chance"),
                new GraphEdge("otlp", "filter"), new GraphEdge("chance", "collector"),
                new GraphEdge("filter", "collector"), new GraphEdge("logging", "collector", "the sink")
            ],
            FlowDirection.Down);

        var placed = GraphLayout.Place(spec);

        var logging = placed.Nodes.Single(node => node.Node.Id == "logging");
        var collector = placed.Nodes.Single(node => node.Node.Id == "collector");
        var numbers = Ways.Numbers(placed.Edges.Single(edge => edge.Edge.Word == "the sink").Way);
        var xs = numbers.Where((_, at) => at % 2 == 0).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(xs.Where(x => Math.Abs(x - logging.Box.Middle.X) > 10 && Math.Abs(x - xs[^1]) > 10),
                Is.Empty, "straight down from its source, then straight into where it arrives");
            Assert.That(xs[^1], Is.InRange(collector.Box.Left, collector.Box.Right), "on its target's side");
        });
    }

    /// <summary>
    /// A box that both sends and receives an arrow back sends from a little above its middle and receives a little
    /// below, so the two never share a stretch (the MCP chart's EngineToolSync).
    /// </summary>
    [Test]
    public void ABoxWithArrowsBackBothWays_SendsAndReceivesApart()
    {
        var spec = new GraphSpec(
            [new GraphNode("client", "Client"), new GraphNode("sync", "Sync"), new GraphNode("engine", "Engine")],
            [
                new GraphEdge("client", "sync"), new GraphEdge("sync", "engine"), new GraphEdge("sync", "client"),
                new GraphEdge("engine", "sync")
            ],
            FlowDirection.Down);

        var placed = GraphLayout.Place(spec);

        var outOf = placed.Edges.Single(edge => edge.Edge is { From: "sync", To: "client" });
        var into = placed.Edges.Single(edge => edge.Edge is { From: "engine", To: "sync" });
        Assert.That(Ways.Points(outOf.Way)[0], Is.Not.EqualTo(Ways.Points(into.Way)[^1]));
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
        double left = edge.Word.X - FlowText.Word.Width(word) / 2;
        Assert.Multiple(() =>
        {
            Assert.That(left, Is.GreaterThan(a.Box.Middle.X), "it starts right of the line");
            Assert.That(edge.Word.X + FlowText.Word.Width(word) / 2, Is.LessThanOrEqualTo(placed.Width));
        });
    }

    /// <summary>
    /// Down, the drawing ends where its boxes and lines end: a long arrow's waypoints take a slot in each row they
    /// pass, but draw nothing, so their slot leaves no empty room on the right (Louis's README chart, CL60.2).
    /// </summary>
    [Test]
    public void ADownChart_EndsWhereWhatItDrawsEnds()
    {
        var spec = new GraphSpec(
            [
                new GraphNode("programs", "The programs"), new GraphNode("sites", "The sites"),
                new GraphNode("packs", "packs/web"), new GraphNode("shared", "louis-agent.web"),
                new GraphNode("api", "louis-agent.api")
            ],
            [
                new GraphEdge("programs", "sites"), new GraphEdge("programs", "api"), new GraphEdge("sites", "packs"),
                new GraphEdge("sites", "shared"), new GraphEdge("packs", "shared"), new GraphEdge("shared", "api")
            ],
            FlowDirection.Down);

        var placed = GraphLayout.Place(spec);

        double drawn = Math.Max(placed.Nodes.Max(node => node.Box.Right),
            placed.Edges.Max(edge => Ways.Xs(edge.Way).Max()));
        Assert.That(placed.Width, Is.LessThanOrEqualTo(Math.Ceiling(drawn)));
    }
}
