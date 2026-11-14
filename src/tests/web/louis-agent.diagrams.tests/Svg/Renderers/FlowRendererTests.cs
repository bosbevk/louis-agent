using LouisAgent.Diagrams.Classes.Spec;
using LouisAgent.Diagrams.Flow.Spec;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Svg;
using LouisAgent.Diagrams.Svg.Renderers.Parts;

namespace LouisAgent.Diagrams.Tests.Svg.Renderers;

/// <summary>
/// A flow is drawn in the new look, and a class chart keeps the plain drawing (F134.8): the two kinds were one
/// drawing, and now change apart.
/// </summary>
public class FlowRendererTests
{
    /// <summary>A host that runs the engine.</summary>
    private static readonly DiagramSource Turn = new(DiagramKind.Flow, "One turn: from a host to the model",
        Flow: new GraphSpec([new GraphNode("host", "A host"), new GraphNode("engine", "The engine")],
            [new GraphEdge("host", "engine", "runs")]));

    /// <summary>A class that uses another.</summary>
    private static readonly DiagramSource Types = new(DiagramKind.Classes, "The speech types",
        Classes: new ClassesSpec([new ClassType("client", "SpeechClient"), new ClassType("text", "Transcript")],
            [new ClassRelation("client", "text", RelationKind.Creates)]));

    /// <summary>A flow is what the new drawing draws: panels in the shared look, and its own title.</summary>
    [Test]
    public void AFlow_IsDrawnInTheNewLook()
    {
        string drawn = DiagramRenderers.Render(Turn);

        Assert.Multiple(() =>
        {
            Assert.That(drawn, Is.EqualTo(FlowDrawing.Of(Turn)));
            Assert.That(drawn, Does.Contain("class=\"pic-title\"").And.Contain("pic-panel"));
        });
    }

    /// <summary>A class chart is still the plain drawing: boxes and joined arrows, and none of the new look.</summary>
    [Test]
    public void AClassChart_KeepsThePlainDrawing()
    {
        string drawn = DiagramRenderers.Render(Types);

        Assert.Multiple(() =>
        {
            Assert.That(drawn, Does.Contain("class=\"box tone-"));
            Assert.That(drawn, Does.Not.Contain("pic-panel").And.Not.Contain("pic-title"));
        });
    }
}
