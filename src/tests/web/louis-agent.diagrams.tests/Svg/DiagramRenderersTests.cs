using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Svg;
using LouisAgent.Diagrams.Icons;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Tests.Components;
using LouisAgent.Diagrams.Tests.Composite;
using LouisAgent.Diagrams.Tickets.Spec;

namespace LouisAgent.Diagrams.Tests.Svg;

/// <summary>
/// CL65.6: the list of renderers is where a picture's geometry is chosen. It hands the one in use to the kind's
/// renderer unless it is told another, so no drawing picks a family of boxes for itself (ADR 0123).
/// </summary>
public class DiagramRenderersTests
{
    /// <summary>A geometry that makes what another makes, and counts how often it was asked.</summary>
    private sealed class Counting(IGeometry inner) : IGeometry
    {
        /// <summary>How many boxes and points it was asked for.</summary>
        public int Asked { get; private set; }

        /// <inheritdoc />
        public IBox Box(double x, double y, double width, double height)
        {
            Asked++;
            return inner.Box(x, y, width, height);
        }

        /// <inheritdoc />
        public IPoint Point(double x, double y)
        {
            Asked++;
            return inner.Point(x, y);
        }

        /// <inheritdoc />
        public IBox Around(IEnumerable<IBox> boxes)
        {
            Asked++;
            return inner.Around(boxes);
        }
    }

    /// <summary>
    /// A picture of three kinds: one that nests boxes, one built on the flow, and the card in a chat.
    /// </summary>
    private static IEnumerable<DiagramSource> Pictures()
    {
        yield return DiagramReader.Read(ComponentSamples.Start);
        yield return DiagramReader.Read(CompositeSamples.Engine);
        yield return new DiagramSource(DiagramKind.Ticket, "A booking", Ticket: new TicketSpec("BOOKING",
            "The garden room", [new TicketRow(DiagramIcon.Calendar, "Dates", "Fri 14 to Sun 16 Nov")]));
    }

    /// <summary>
    /// A picture drawn with a geometry it is told has its boxes and points made by that one, and is the same
    /// picture the geometry in use draws when the two make the same boxes.
    /// </summary>
    [TestCaseSource(nameof(Pictures))]
    public void APicture_IsDrawnWithTheGeometryItIsTold(DiagramSource source)
    {
        var counting = new Counting(Geometries.InUse);

        string drawn = DiagramRenderers.Render(source, counting);

        Assert.Multiple(() =>
        {
            Assert.That(counting.Asked, Is.GreaterThan(0), "the picture's boxes were made by the geometry it was told");
            Assert.That(drawn, Is.EqualTo(DiagramRenderers.Render(source)));
        });
    }
}
