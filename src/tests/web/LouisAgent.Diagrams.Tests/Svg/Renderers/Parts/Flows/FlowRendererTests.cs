using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Svg;
using LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.Flows;
using LouisAgent.Diagrams.Flow.Spec;
using LouisAgent.Diagrams.Sources;

namespace LouisAgent.Diagrams.Tests.Svg.Renderers.Parts.Flows;

/// <summary>
/// A flow is drawn in the new look (F134.8), through the kind's one renderer.
/// </summary>
public class FlowRendererTests
{
    /// <summary>A host that runs the engine.</summary>
    private static readonly DiagramSource Turn = new(DiagramKind.Flow, "One turn: from a host to the model",
        Flow: new GraphSpec([new GraphNode("host", "A host"), new GraphNode("engine", "The engine")],
            [new GraphEdge("host", "engine", "runs")]));

    /// <summary>A flow is what the new drawing draws: panels in the shared look, and its small title.</summary>
    [Test]
    public void AFlow_IsDrawnInTheNewLook()
    {
        string drawn = DiagramRenderers.Render(Turn);

        Assert.Multiple(() =>
        {
            Assert.That(drawn, Is.EqualTo(FlowDrawing.Of(Turn, Geometries.InUse)));
            Assert.That(drawn, Does.Contain("class=\"mrq-title\"").And.Contain("pic-panel"));
        });
    }
}
