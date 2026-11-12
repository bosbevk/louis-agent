using System.Xml.Linq;
using LouisAgent.Diagrams.Flow;
using LouisAgent.Diagrams.Flow.Placed;
using LouisAgent.Diagrams.Flow.Spec;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Svg;

namespace LouisAgent.Diagrams.Tests.Flow;

/// <summary>
/// Groups and dashed arrows (CL60.2), as the docs' subgraphs and dotted arrows: a group is a labelled frame around
/// its boxes, and no box of another group ever stands inside it.
/// </summary>
public class FlowGroupsTests
{
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

    /// <summary>
    /// A developer outside, the system's host, engine and tools in one group, and its store in another, the arrow to
    /// the store dashed.
    /// </summary>
    private static GraphSpec System(FlowDirection direction) => new(
        [
            new GraphNode("dev", "Developer"),
            new GraphNode("host", "A host", Group: "system"),
            new GraphNode("engine", "The engine", Group: "system"),
            new GraphNode("tools", "The tools", Group: "system"),
            new GraphNode("store", "The ledger", Group: "data")
        ],
        [
            new GraphEdge("dev", "host"),
            new GraphEdge("host", "engine"),
            new GraphEdge("engine", "tools"),
            new GraphEdge("engine", "store", "writes", EdgeStyle.Dashed)
        ],
        direction,
        [new GraphGroup("system", "louis-agent"), new GraphGroup("data", "logs/")]);

    /// <summary>The groups, drawn, match their golden file.</summary>
    [Test]
    public void Groups_MatchTheGoldenFile() =>
        Golden.Match("groups.svg", DiagramRenderers.Render(
            new DiagramSource(DiagramKind.Flow, "A system in groups", Flow: System(FlowDirection.Right))));

    /// <summary>Every group gets one frame, and each of its boxes stands inside it.</summary>
    [TestCase(FlowDirection.Right)]
    [TestCase(FlowDirection.Down)]
    public void EveryMember_StandsInsideItsFrame(FlowDirection direction)
    {
        var placed = GraphLayout.Place(System(direction));

        Assert.That(placed.Groups.Select(group => group.Group.Id), Is.EquivalentTo(new[] { "system", "data" }));
        Assert.Multiple(() =>
        {
            foreach (var node in placed.Nodes.Where(node => node.Node.Group is not null))
                Assert.That(Inside(node, placed.Groups.Single(group => group.Group.Id == node.Node.Group)), Is.True,
                    node.Node.Id);
        });
    }

    /// <summary>No box of another group, and no box outside every group, stands inside a frame.</summary>
    [TestCase(FlowDirection.Right)]
    [TestCase(FlowDirection.Down)]
    public void NoOtherBox_StandsInsideAFrame(FlowDirection direction)
    {
        var placed = GraphLayout.Place(System(direction));

        Assert.Multiple(() =>
        {
            foreach (var group in placed.Groups)
                foreach (var node in placed.Nodes.Where(node => node.Node.Group != group.Group.Id))
                    Assert.That(Overlap(node, group), Is.False, $"{node.Node.Id} in {group.Group.Id}");
        });
    }

    /// <summary>Two frames never overlap.</summary>
    [TestCase(FlowDirection.Right)]
    [TestCase(FlowDirection.Down)]
    public void TwoFrames_NeverOverlap(FlowDirection direction)
    {
        var groups = GraphLayout.Place(System(direction)).Groups;

        Assert.That(Overlap(groups[0], groups[1]), Is.False);
    }

    /// <summary>A dashed arrow is drawn dashed, the others solid; a group's frame and its label are drawn.</summary>
    [Test]
    public void ADashedArrow_IsDrawnDashed_AndAGroupFramed()
    {
        var svg = XDocument.Parse(DiagramRenderers.Render(
            new DiagramSource(DiagramKind.Flow, "A system in groups", Flow: System(FlowDirection.Right))));

        var edges = svg.Descendants(Svg + "path").Select(path => (string?)path.Attribute("class"))
            .Where(cls => cls?.StartsWith("edge", StringComparison.Ordinal) == true).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(edges.Count(cls => cls == "edge dashed"), Is.EqualTo(1));
            Assert.That(edges.Count(cls => cls == "edge"), Is.EqualTo(3));
            Assert.That(svg.Descendants(Svg + "rect").Count(rect => (string?)rect.Attribute("class") == "group"),
                Is.EqualTo(2));
            Assert.That(svg.Descendants(Svg + "text").Select(text => text.Value), Does.Contain("louis-agent"));
        });
    }

    /// <summary>
    /// Down, a band's boxes stand as far apart as its own widest box needs, not the widest box of the whole chart.
    /// </summary>
    [Test]
    public void ABandsRows_FitItsOwnBoxes()
    {
        var spec = new GraphSpec(
            [
                new GraphNode("a", "A"), new GraphNode("b", "B"),
                new GraphNode("wide", "a label long enough to make its box far wider than the others", Group: "g")
            ],
            [new GraphEdge("a", "wide"), new GraphEdge("b", "wide")],
            FlowDirection.Down,
            [new GraphGroup("g", "A group")]);

        var at = GraphLayout.Place(spec).Nodes.ToDictionary(node => node.Node.Id);

        Assert.That(Math.Abs(at["b"].X - at["a"].X), Is.LessThan(at["wide"].W));
    }

    /// <summary>
    /// Arrows from outside into a group bend outside its frame, not along its margin (Louis, of the checks chart).
    /// </summary>
    [Test]
    public void ArrowsIntoAGroup_BendOutsideItsFrame()
    {
        var spec = new GraphSpec(
            [
                new GraphNode("general", "the general checks"), new GraphNode("pack", "a pack's checks"),
                new GraphNode("catalog", "CheckCatalog", Group: "project"),
                new GraphNode("check", "ICheck", Group: "project")
            ],
            [
                new GraphEdge("general", "catalog", "lists its entries"),
                new GraphEdge("pack", "catalog", "lists its entries"),
                new GraphEdge("catalog", "check")
            ],
            FlowDirection.Down,
            [new GraphGroup("project", "This project")]);

        var placed = GraphLayout.Place(spec);

        var frame = placed.Groups.Single();
        var into = placed.Edges.First(edge => edge.Edge.To == "catalog" && edge.Path.Contains('Q'));
        // The bend is the height the path runs along at: the one y two of its points share.
        var ys = into.Path.Split(' ').Where(part => double.TryParse(part, out _)).Select(double.Parse)
            .Where((_, at) => at % 2 == 1).ToList();
        double bend = ys.GroupBy(y => y).Where(same => same.Count() > 1).Select(same => same.Key).Single();
        Assert.That(bend, Is.LessThan(frame.Y), "it runs along above the frame, then drops in");
    }

    /// <summary>A diagram without groups has no frame, so the site's diagrams draw as before.</summary>
    [Test]
    public void NoGroups_NoFrames() =>
        Assert.That(GraphLayout.Place(System(FlowDirection.Right) with { Groups = null }).Groups, Is.Empty);

    /// <summary>Whether a box stands wholly inside a frame.</summary>
    private static bool Inside(PlacedNode node, PlacedGroup group) =>
        node.X >= group.X && node.Y >= group.Y && node.X + node.W <= group.X + group.W
        && node.Y + node.H <= group.Y + group.H;

    /// <summary>Whether a box and a frame share any ground.</summary>
    private static bool Overlap(PlacedNode node, PlacedGroup group) =>
        node.X < group.X + group.W && group.X < node.X + node.W && node.Y < group.Y + group.H
        && group.Y < node.Y + node.H;

    /// <summary>Whether two frames share any ground.</summary>
    private static bool Overlap(PlacedGroup a, PlacedGroup b) =>
        a.X < b.X + b.W && b.X < a.X + a.W && a.Y < b.Y + b.H && b.Y < a.Y + a.H;
}
