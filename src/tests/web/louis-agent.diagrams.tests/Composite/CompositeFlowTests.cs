using LouisAgent.Diagrams.Engine.Composite;
using LouisAgent.Diagrams.Engine.Flow;
using LouisAgent.Diagrams.Engine.Flow.Placed;
using LouisAgent.Diagrams.Flow.Spec;
using LouisAgent.Diagrams.Sources;

namespace LouisAgent.Diagrams.Tests.Composite;

/// <summary>
/// F134.13: a composite structure diagram has no layout of its own. An adapter turns it into a flow, and the
/// flow's layout places it: what is asked of the class on the left, its parts in the middle by which uses which,
/// and what it needs on the right.
/// </summary>
public class CompositeFlowTests
{
    /// <summary>The shared sample as a flow.</summary>
    private static GraphSpec Flow() => CompositeFlow.Of(DiagramReader.Read(CompositeSamples.Engine).Composite!);

    /// <summary>The sample as the flow's layout places it, by the rules a drawing uses.</summary>
    private static PlacedGraph Placed() =>
        GraphLayout.Place(Flow(), bend: GraphLayout.SoftBend, rules: FlowRules.Straight);

    /// <summary>A placed box, by its id.</summary>
    private static PlacedNode Box(PlacedGraph placed, string id) => placed.Nodes.Single(node => node.Node.Id == id);

    /// <summary>
    /// Every part and every port is a box, all in one group, which is the class. A port's box has an id no part
    /// can have.
    /// </summary>
    [Test]
    public void EveryPartAndPort_IsABox_InTheOneGroup()
    {
        var flow = Flow();

        Assert.Multiple(() =>
        {
            Assert.That(flow.Nodes.Select(node => node.Id), Is.EquivalentTo(new[]
            {
                "turns", "guards", "registry", "commands", CompositeFlow.PortId("prompts"),
                CompositeFlow.PortId("typed"), CompositeFlow.PortId("model")
            }));
            Assert.That(flow.Nodes.Select(node => node.Group), Is.All.EqualTo(CompositeFlow.Class));
            Assert.That(flow.Groups!.Select(group => group.Label), Is.EqualTo(new[] { "AgentEngine" }));
            Assert.That(CompositeFlow.PortId("turns"), Is.Not.EqualTo("turns"), "a port named as a part is told apart");
        });
    }

    /// <summary>
    /// What is asked of the class stands in the first column and what it needs in the last. A part stands after
    /// the parts that use it: the turns before the guards, and both before the registry.
    /// </summary>
    [Test]
    public void WhatIsAskedStandsFirst_ThePartsByWhichUsesWhich_WhatIsNeededLast()
    {
        var column = Flow().Nodes.ToDictionary(node => node.Id, node => node.Column);
        int? last = column[CompositeFlow.PortId("model")];

        Assert.Multiple(() =>
        {
            Assert.That(column[CompositeFlow.PortId("prompts")], Is.Zero);
            Assert.That(column[CompositeFlow.PortId("typed")], Is.Zero);
            Assert.That(new[] { column["turns"], column["guards"], column["registry"] },
                Is.EqualTo(new int?[] { 1, 2, 3 }));
            Assert.That(column["commands"], Is.EqualTo(1), "a part nothing uses stands with the first");
            Assert.That(last, Is.EqualTo(4), "one past the last column of parts");
        });
    }

    /// <summary>
    /// An arrow a port: from what is asked to the part that does it, and from the part that needs something to
    /// what it needs. A connector is an arrow from the part that uses to the part it uses, with its word.
    /// </summary>
    [Test]
    public void APortIsAnArrow_AndSoIsAConnector()
    {
        var edges = Flow().Edges.Select(edge => (edge.From, edge.To, edge.Word)).ToList();

        Assert.That(edges, Is.EquivalentTo(new (string, string, string?)[]
        {
            (CompositeFlow.PortId("prompts"), "turns", null), (CompositeFlow.PortId("typed"), "commands", null),
            ("guards", CompositeFlow.PortId("model"), null), ("turns", "guards", "the pipeline"),
            ("turns", "registry", null), ("guards", "registry", null)
        }));
    }

    /// <summary>
    /// A box is as wide as what will be drawn in it: a part's type with its role beside it, and a port's words
    /// with its pin's name. So the label the layout measures holds both.
    /// </summary>
    [Test]
    public void ABoxsLabel_HoldsBothThingsDrawnInIt()
    {
        var label = Flow().Nodes.ToDictionary(node => node.Id, node => node.Label);

        Assert.Multiple(() =>
        {
            Assert.That(label["turns"], Does.Contain("TurnRunner").And.Contain("turns"));
            Assert.That(label[CompositeFlow.PortId("prompts")],
                Does.Contain("ProcessPromptAsync").And.Contain("prompts"));
            Assert.That(label.Values, Has.None.Contains("\n"), "one line, so a box is one line tall");
        });
    }

    /// <summary>
    /// Placed by the flow's layout, the picture reads left to right, and a port's line starts or ends at the side
    /// of its box that faces the parts, which is where the drawing puts its pin.
    /// </summary>
    [Test]
    public void PlacedByTheFlowsLayout_APortsLineStartsOrEndsAtItsBox()
    {
        var placed = Placed();
        var asked = Box(placed, CompositeFlow.PortId("prompts"));
        var needed = Box(placed, CompositeFlow.PortId("model"));
        string into = placed.Edges.Single(edge => edge.Edge.From == asked.Node.Id).Way.Text;
        string outOf = placed.Edges.Single(edge => edge.Edge.To == needed.Node.Id).Way.Text;

        Assert.Multiple(() =>
        {
            Assert.That(asked.Box.Left, Is.LessThan(Box(placed, "turns").Box.Left));
            Assert.That(needed.Box.Left, Is.GreaterThan(Box(placed, "registry").Box.Left));
            Assert.That(placed.Groups, Has.Count.EqualTo(1));
            Assert.That(into, Does.StartWith(FormattableString.Invariant(
                $"M {asked.Box.Right:0.#} {asked.Box.Middle.Y:0.#} ")));
            Assert.That(outOf, Does.EndWith(FormattableString.Invariant(
                $" {needed.Box.Left:0.#} {needed.Box.Middle.Y:0.#}")));
        });
    }
}
