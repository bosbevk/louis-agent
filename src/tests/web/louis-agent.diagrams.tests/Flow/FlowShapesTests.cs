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
