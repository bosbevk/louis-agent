using System.Globalization;
using LouisAgent.Diagrams.Flow.Shapes;
using LouisAgent.Diagrams.Flow.Spec;
using LouisAgent.Web.Components.Graphs;

namespace LouisAgent.Web.Tests.Components.Graphs;

/// <summary>
/// The view draws a diagram from its data (F111): one path per arrow, one group per box, a word where an arrow has
/// one, the root marked for the frame's reveal, and the markup pinned to its golden file.
/// </summary>
public class GraphViewTests
{
    /// <summary>The Identities page's shape: one box on the left, three on the right.</summary>
    private static GraphSpec Identity() => new(
        [
            new GraphNode("who", "An identity", "who the agent is", 0),
            new GraphNode("voice", "A voice", null, 1),
            new GraphNode("roles", "Roles", null, 1),
            new GraphNode("packs", "The packs it names", null, 1)
        ],
        [new GraphEdge("who", "voice"), new GraphEdge("who", "roles"), new GraphEdge("who", "packs", "names")]);

    private static IRenderedComponent<GraphView> Render(BunitContext ctx) => Render(ctx, Identity());

    private static IRenderedComponent<GraphView> Render(BunitContext ctx, GraphSpec spec) =>
        ctx.Render<GraphView>(parameters =>
            parameters.Add(view => view.Spec, spec).Add(view => view.Title, "An identity, as files"));

    /// <summary>Two boxes that feed one, the lower arrow with a word, and the box they feed in a shape.</summary>
    private static GraphSpec TwoIntoOne(NodeShape shape) => new(
        [
            new GraphNode("a", "A", Column: 0), new GraphNode("b", "B", Column: 0),
            new GraphNode("x", "Ready?", Column: 1, Shape: shape)
        ],
        [new GraphEdge("a", "x"), new GraphEdge("b", "x", "asks")]);

    /// <summary>The numbers of a path or an attribute, in the order they stand.</summary>
    private static List<double> Numbers(string text) =>
    [
        .. text.Split(' ', 'M', 'L', 'Q', 'Z').Where(part => part.Length > 0)
            .Select(part => double.Parse(part, CultureInfo.InvariantCulture))
    ];

    /// <summary>
    /// Every box is a group, every arrow a path, the one word on its arrow, the root ready to reveal.
    /// </summary>
    [Test]
    public void TheDiagram_HasABoxAndAnArrowForEach()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx);

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll("figure.graph.reveal"), Has.Count.EqualTo(1));
            Assert.That(cut.FindAll("g.node"), Has.Count.EqualTo(4));
            Assert.That(cut.FindAll("path.edge"), Has.Count.EqualTo(3));
            Assert.That(cut.Find("text.word").TextContent, Is.EqualTo("names"));
            Assert.That(cut.Find("figure").GetAttribute("aria-label"), Is.EqualTo("An identity, as files"));
            Assert.That(cut.Find("figcaption").TextContent, Is.EqualTo("An identity, as files"));
        });
    }

    /// <summary>Each box and arrow carries its column, so the styles can delay the columns in turn.</summary>
    [Test]
    public void TheColumns_AreOnTheParts()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx);

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll("g.node").Select(node => node.GetAttribute("style")),
                Is.EquivalentTo(new[] { "--col: 0", "--col: 1", "--col: 1", "--col: 1" }));
            Assert.That(cut.FindAll("path.edge").Select(edge => edge.GetAttribute("style")),
                Has.All.Contains("--len: ").And.All.EndsWith("--col: 0"));
        });
    }

    /// <summary>
    /// Two arrows into one box arrive at a point each (F134.8), so you can follow which box feeds it: the site's
    /// diagrams are laid out by the rules a flow in the docs is.
    /// </summary>
    [Test]
    public void TwoArrowsIntoABox_ArriveAtTheirOwnPoints()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx, TwoIntoOne(NodeShape.Box));
        var ends = cut.FindAll("path.edge").Select(edge => Numbers(edge.GetAttribute("d")!)[^1]).ToList();

        Assert.That(Math.Abs(ends[0] - ends[1]), Is.EqualTo(18), "a step apart on the box's side");
    }

    /// <summary>
    /// An arrow's word stands on its own line, the line running behind it, so it never sits on the arrow a step
    /// above. Its letters stand 3.5 under the line's middle, which puts a 10 px word's middle on the line.
    /// </summary>
    [Test]
    public void AWord_StandsOnItsLine()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx, TwoIntoOne(NodeShape.Box));
        double leaves = Numbers(cut.FindAll("path.edge")[1].GetAttribute("d")!)[1];

        Assert.That(double.Parse(cut.Find("text.word").GetAttribute("y")!, CultureInfo.InvariantCulture),
            Is.EqualTo(leaves + 3.5));
    }

    /// <summary>
    /// A word has a ground behind it, as wide as the word and round the line's height, so the line it stands on
    /// doesn't show through the space between two words.
    /// </summary>
    [Test]
    public void AWord_HasAGroundThatHidesItsLine()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx, TwoIntoOne(NodeShape.Box));
        var ground = cut.Find("rect.word-ground");
        double Of(string name) => double.Parse(ground.GetAttribute(name)!, CultureInfo.InvariantCulture);
        double leaves = Numbers(cut.FindAll("path.edge")[1].GetAttribute("d")!)[1];

        Assert.Multiple(() =>
        {
            Assert.That(Of("y") + Of("height") / 2, Is.EqualTo(leaves), "its middle is on the line");
            Assert.That(Of("width"), Is.GreaterThanOrEqualTo("asks".Length * 7));
            Assert.That(ground.GetAttribute("style"), Does.Contain("--col: 0"), "it appears with its word");
        });
    }

    /// <summary>
    /// A decision is drawn as its outline, a box with pointed ends and six corners, and not as a box.
    /// </summary>
    [Test]
    public void ADecision_IsDrawnAsItsOutline()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx, TwoIntoOne(NodeShape.Decision));
        var decision = cut.FindAll("g.node")[2];

        Assert.Multiple(() =>
        {
            Assert.That(decision.QuerySelectorAll("rect"), Is.Empty);
            Assert.That(Numbers(decision.QuerySelector("path")!.GetAttribute("d")!), Has.Count.EqualTo(12));
            Assert.That(Numbers(decision.QuerySelector("path")!.GetAttribute("d")!).Min(), Is.Zero,
                "drawn from the group's own corner");
        });
    }

    /// <summary>A start or an end is a pill: a box whose corners are half its height.</summary>
    [Test]
    public void AStartOrAnEnd_IsAPill()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx, TwoIntoOne(NodeShape.Terminal));
        var pill = cut.FindAll("g.node")[2].QuerySelector("rect")!;

        Assert.That(Numbers(pill.GetAttribute("rx")!)[0], Is.EqualTo(Numbers(pill.GetAttribute("height")!)[0] / 2));
    }

    /// <summary>The markup matches the golden file.</summary>
    [Test]
    public void TheDiagram_MatchesTheGoldenFile()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx);

        Snapshots.Match("Components/Graphs/GraphView.html", cut.Markup);
    }
}
