using System.Xml.Linq;
using LouisAgent.Diagrams.Engine.Flow;
using LouisAgent.Diagrams.Engine.Flow.Placed;
using LouisAgent.Diagrams.Flow.Spec;
using LouisAgent.Diagrams.Tests.Flow.Arrows;

namespace LouisAgent.Diagrams.Tests.Flow;

/// <summary>
/// Groups and dashed arrows (CL60.2), as the docs' subgraphs and dotted arrows: a group is a labelled frame around
/// its boxes, and no box of another group ever stands inside it.
/// </summary>
public class FlowGroupsTests
{
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

        Assert.That(Math.Abs(at["b"].Box.Left - at["a"].Box.Left), Is.LessThan(at["wide"].Box.Width));
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
        var into = placed.Edges.First(edge => edge.Edge.To == "catalog" && Ways.Turns(edge.Way) > 0);
        // The bend is the height the path runs along at: the one y two of its points share.
        var ys = Ways.Ys(into.Way);
        double bend = ys.GroupBy(y => y).Where(same => same.Count() > 1).Select(same => same.Key).Single();
        Assert.That(bend, Is.LessThan(frame.Box.Top), "it runs along above the frame, then drops in");
    }

    /// <summary>A diagram without groups has no frame, so the site's diagrams draw as before.</summary>
    [Test]
    public void NoGroups_NoFrames() =>
        Assert.That(GraphLayout.Place(System(FlowDirection.Right) with { Groups = null }).Groups, Is.Empty);

    /// <summary>Whether a box stands wholly inside a frame.</summary>
    private static bool Inside(PlacedNode node, PlacedGroup group) =>
        node.Box.Left >= group.Box.Left && node.Box.Top >= group.Box.Top && node.Box.Right <= group.Box.Right
        && node.Box.Bottom <= group.Box.Bottom;

    /// <summary>Whether a box and a frame share any ground.</summary>
    private static bool Overlap(PlacedNode node, PlacedGroup group) =>
        node.Box.Left < group.Box.Right && group.Box.Left < node.Box.Right && node.Box.Top < group.Box.Bottom
        && group.Box.Top < node.Box.Bottom;

    /// <summary>Whether two frames share any ground.</summary>
    private static bool Overlap(PlacedGroup a, PlacedGroup b) =>
        a.Box.Left < b.Box.Right && b.Box.Left < a.Box.Right && a.Box.Top < b.Box.Bottom && b.Box.Top < a.Box.Bottom;
}
