using System.Xml.Linq;
using LouisAgent.Diagrams.Flow.Spec;
using LouisAgent.Diagrams.Icons;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Svg;
using LouisAgent.Diagrams.Tickets.Spec;

namespace LouisAgent.Diagrams.Tests.Svg;

/// <summary>
/// A drawing put in a page rather than a file (F127.4.1): the page holds it inline, so its colours can come from the
/// page, but its styles reach only the drawing itself, and it brings no id a second drawing on the page would clash
/// with when it draws no arrow.
/// </summary>
public class PageSvgTests
{
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

    /// <summary>A small ticket: one row is enough.</summary>
    private static DiagramSource Ticket() => new(DiagramKind.Ticket, "BOOKING REQUEST: The garden room",
        Ticket: new TicketSpec("BOOKING REQUEST", "The garden room",
            [new TicketRow(DiagramIcon.Calendar, "Dates", "Fri 14 to Sun 16 Nov")]));

    /// <summary>A small flow, with an arrow.</summary>
    private static DiagramSource Flow() => new(DiagramKind.Flow, "One turn", null,
        new GraphSpec([new GraphNode("host", "A host", null, 0), new GraphNode("model", "The model", null, 1)],
            [new GraphEdge("host", "model", "asks")]));

    /// <summary>
    /// Every style sits inside <c>@scope</c>, so the browser keeps its rules to this drawing: a page's own
    /// <c>.title</c> or <c>.label</c> never takes the drawing's font.
    /// </summary>
    [Test]
    public void EveryStyle_IsScopedToTheDrawing()
    {
        var styles = XElement.Parse(DiagramRenderers.RenderForPage(Ticket())).Elements(Svg + "style").ToList();

        Assert.Multiple(() =>
        {
            Assert.That(styles, Has.Count.EqualTo(2));
            Assert.That(styles.Select(style => style.Value.TrimStart()), Has.All.StartsWith("@scope {"));
            Assert.That(styles.Select(style => style.Value.TrimEnd()), Has.All.EndsWith("}"));
        });
    }

    /// <summary>
    /// The colours are set on the drawing itself: inside <c>@scope</c> a bare <c>svg</c> means an svg inside it, which
    /// misses the root and leaves every colour unset (the card drew black on the PC), so the root is <c>:scope</c>.
    /// </summary>
    [Test]
    public void TheColours_AreSetOnTheDrawingItself()
    {
        var styles = XElement.Parse(DiagramRenderers.RenderForPage(Ticket())).Elements(Svg + "style")
            .Select(style => style.Value).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(styles, Has.All.Contain(":scope {"));
            Assert.That(styles, Has.None.Match(@"(^|\n)\s*svg\s*\{"));
        });
    }

    /// <summary>A drawing with no arrow brings no arrowhead, so two tickets on a page hold no id twice.</summary>
    [Test]
    public void ADrawingWithNoArrow_BringsNoArrowhead() =>
        Assert.That(DiagramRenderers.RenderForPage(Ticket()), Does.Not.Contain("<marker"));

    /// <summary>A drawing with arrows keeps the arrowhead they point with.</summary>
    [Test]
    public void ADrawingWithArrows_KeepsItsArrowhead() =>
        Assert.That(DiagramRenderers.RenderForPage(Flow()), Does.Contain("<marker"));

    /// <summary>The file for the docs stays as it was: its styles plain, read by any viewer.</summary>
    [Test]
    public void TheFile_KeepsItsPlainStyles() =>
        Assert.That(DiagramRenderers.Render(Ticket()), Does.Not.Contain("@scope"));
}
