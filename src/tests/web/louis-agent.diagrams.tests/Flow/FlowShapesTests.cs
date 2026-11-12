using System.Globalization;
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
        var numbers = past.Path.Split(' ').Where(part => double.TryParse(part, out _)).Select(double.Parse).ToList();
        var chat = placed.Nodes.Single(node => node.Node.Id == "chat");
        Assert.Multiple(() =>
        {
            Assert.That(placed.Nodes.Select(node => node.Node.Id),
                Is.EquivalentTo(new[] { "host", "chat", "engine", "out" }));
            Assert.That(placed.Edges.Count(edge => edge.Edge is { From: "host", To: "engine" }), Is.EqualTo(1));
            Assert.That(numbers.Where((_, at) => at % 2 == 1).Max(), Is.LessThan(placed.Height), "no lane under it");
            Assert.That(numbers.Where((_, at) => at % 2 == 0)
                .All(x => x < chat.X || x > chat.X + chat.W), Is.True, "it passes beside the box, not through it");
        });
    }

    /// <summary>
    /// Where several boxes feed several others across one gap, each source bends on a track of its own, so a reader
    /// can follow who feeds whom; where they all feed one box, they join on one track as before.
    /// </summary>
    [Test]
    public void SeveralSourcesToSeveralTargets_BendOnTracksOfTheirOwn()
    {
        var spec = new GraphSpec(
            [
                new GraphNode("eval", "IEval"), new GraphNode("session", "ISessionClient"),
                new GraphNode("datasets", "Datasets"), new GraphNode("mode", "EvalMode"),
                new GraphNode("http", "HttpSessionClient"), new GraphNode("canned", "CannedSessionClient")
            ],
            [
                new GraphEdge("eval", "datasets"), new GraphEdge("eval", "mode"),
                new GraphEdge("session", "http"), new GraphEdge("session", "canned")
            ]);

        var placed = GraphLayout.Place(spec);

        // A bent connector's track is its first rounded corner: the point after its first Q.
        double Track(string from) => placed.Edges.Where(edge => edge.Edge.From == from && edge.Path.Contains('Q'))
            .Select(edge => edge.Path.Split(' '))
            .Select(parts => double.Parse(parts[Array.IndexOf(parts, "Q") + 1],
                System.Globalization.CultureInfo.InvariantCulture))
            .Distinct().Single();

        Assert.That(Track("eval"), Is.Not.EqualTo(Track("session")));
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
        var numbers = placed.Edges.Single(edge => edge.Edge.Word == "AgentHost.Build").Path.Split(' ')
            .Where(part => double.TryParse(part, out _)).Select(double.Parse).ToList();
        // Out of its source, along, and straight down into the target: its upright pieces stand at two places only,
        // the source's middle and the target's, with no jog through a slot between (a curve's corners left out).
        var host = placed.Nodes.Single(node => node.Node.Id == "host");
        var xs = numbers.Where((_, at) => at % 2 == 0).Select(x => Math.Round(x)).ToList();
        Assert.That(
            xs.Where(x => Math.Abs(x - (host.X + host.W / 2)) > 10 && Math.Abs(x - (engine.X + engine.W / 2)) > 10),
            Is.Empty);
    }

    /// <summary>
    /// A long arrow whose target's line is blocked drops straight from its source instead, and turns at the end
    /// (Louis, of the telemetry chart).
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
        var numbers = placed.Edges.Single(edge => edge.Edge.Word == "the sink").Path.Split(' ')
            .Where(part => double.TryParse(part, out _)).Select(double.Parse).ToList();
        var xs = numbers.Where((_, at) => at % 2 == 0).ToList();
        Assert.That(xs.Where(x => Math.Abs(x - (logging.X + logging.W / 2)) > 10
                                  && Math.Abs(x - (collector.X + collector.W / 2)) > 10), Is.Empty);
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

        string First(string path) => string.Join(' ', path.Split(' ').Skip(1).Take(2));
        string Last(string path) => string.Join(' ', path.Split(' ').TakeLast(2));
        var outOf = placed.Edges.Single(edge => edge.Edge is { From: "sync", To: "client" });
        var into = placed.Edges.Single(edge => edge.Edge is { From: "engine", To: "sync" });
        Assert.That(First(outOf.Path), Is.Not.EqualTo(Last(into.Path)));
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

        double drawn = Math.Max(placed.Nodes.Max(node => node.X + node.W),
            placed.Edges.Max(edge => Xs(edge.Path).Max()));
        Assert.That(placed.Width, Is.LessThanOrEqualTo(Math.Ceiling(drawn)));
    }

    /// <summary>Every x a path passes: the first number of each pair after its commands.</summary>
    private static IEnumerable<double> Xs(string path) => path.Split(' ')
        .Where(part => double.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
        .Select(part => double.Parse(part, CultureInfo.InvariantCulture)).Where((_, at) => at % 2 == 0);

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
