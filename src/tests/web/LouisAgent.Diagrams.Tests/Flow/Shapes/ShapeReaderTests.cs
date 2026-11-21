using LouisAgent.Diagrams.Flow.Shapes;
using LouisAgent.Diagrams.Sources;

namespace LouisAgent.Diagrams.Tests.Flow.Shapes;

/// <summary>
/// A box names its shape in the diagram's file (F134.8). A name that is no shape is refused with the shapes there
/// are, so a typo never becomes a plain box that looks as if it was meant.
/// </summary>
public class ShapeReaderTests
{
    /// <summary>A flow of one box, with whatever is written for its shape.</summary>
    private static string Flow(string shape) => $$"""
                                                  {
                                                    "kind": "flow",
                                                    "type": "activity",
                                                    "title": "A check",
                                                    "flow": {
                                                      "nodes": [ { "id": "ok", "label": "Tests pass?"{{shape}} } ],
                                                      "edges": []
                                                    }
                                                  }
                                                  """;

    /// <summary>A box that names no shape is a plain box.</summary>
    [Test]
    public void ABoxThatNamesNoShape_IsPlain()
    {
        Assert.That(DiagramReader.Read(Flow("")).Flow!.Nodes[0].Shape, Is.EqualTo(NodeShape.Box));
    }

    /// <summary>Each shape is read by its name.</summary>
    [TestCase("decision", NodeShape.Decision)]
    [TestCase("data", NodeShape.Data)]
    [TestCase("terminal", NodeShape.Terminal)]
    public void AShape_IsReadByItsName(string name, NodeShape shape)
    {
        string json = Flow($", \"shape\": \"{name}\"");

        Assert.That(DiagramReader.Read(json).Flow!.Nodes[0].Shape, Is.EqualTo(shape));
    }

    /// <summary>A name that is no shape is refused, and the refusal lists the shapes there are.</summary>
    [Test]
    public void ANameThatIsNoShape_IsRefusedWithTheShapesThereAre()
    {
        string json = Flow(", \"shape\": \"hexagon\"");

        var refusal = Assert.Throws<InvalidDataException>(() => DiagramReader.Read(json));

        Assert.That(refusal!.Message, Does.Contain("hexagon").And.Contain("box, decision, data, terminal"));
    }

    /// <summary>A shape is written back by its name, and a plain box's is left out.</summary>
    [Test]
    public void AShape_IsWrittenByItsName()
    {
        string decision = DiagramWriter.Write(DiagramReader.Read(Flow(", \"shape\": \"decision\"")));
        string plain = DiagramWriter.Write(DiagramReader.Read(Flow("")));

        Assert.Multiple(() =>
        {
            Assert.That(decision, Does.Contain("\"shape\": \"decision\""));
            Assert.That(plain, Does.Not.Contain("shape"));
        });
    }
}
