using System.Globalization;
using System.Xml.Linq;
using LouisAgent.Diagrams.Cards;
using LouisAgent.Diagrams.Cards.Links;
using LouisAgent.Diagrams.Engine.Flow;
using LouisAgent.Diagrams.Engine.Svg;
using LouisAgent.Diagrams.Engine.Svg.Fits;
using LouisAgent.Diagrams.Flow.Spec;
using LouisAgent.Diagrams.Icons;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Tickets.Spec;

namespace LouisAgent.Diagrams.Tests.Svg.Fits;

/// <summary>
/// F169.5 (Louis's picks "A: both drawn for a phone" and "A: both in the page, CSS shows one"): the engine answers
/// for any picture whether it has a narrow form, a flow and a cards picture running down, and draws a page's copy
/// in the fit it is asked for.
/// </summary>
public class NarrowFitTests
{
    /// <summary>A request's path to rand, as the framework's cost page draws it: three across and three back.</summary>
    private static readonly DiagramSource CostPath = DiagramReader.Read("""
        {
          "kind": "flow", "type": "activity", "title": "A request's path to rand",
          "flow": {
            "direction": "right", "rowGap": 56,
            "nodes": [
              { "id": "request", "label": "One request\nclaude-sonnet-5-5", "icon": "message", "tone": "blue",
                "shape": "terminal", "column": 0 },
              { "id": "tokens", "label": "Its tokens\n12000 in, 800 out", "icon": "chip", "tone": "violet",
                "column": 1 },
              { "id": "prices", "label": "prices.json\n2026-11-01", "icon": "file", "tone": "amber",
                "shape": "data", "column": 2 },
              { "id": "cost", "label": "The cost\n$0.048", "icon": "chart", "tone": "violet", "column": 2 },
              { "id": "ledger", "label": "The ledger\nkept in dollars", "icon": "database", "tone": "amber",
                "shape": "data", "column": 1 },
              { "id": "rand", "label": "Shown in rand\nR0.86", "icon": "browser", "tone": "violet",
                "shape": "terminal", "column": 0 }
            ],
            "edges": [
              { "from": "request", "to": "tokens" },
              { "from": "tokens", "to": "prices", "word": "a million at a time" },
              { "from": "prices", "to": "cost" }, { "from": "cost", "to": "ledger" },
              { "from": "ledger", "to": "rand", "word": "R18 a dollar" }
            ]
          }
        }
        """);

    /// <summary>A headline and the two cards it feeds.</summary>
    private static DiagramSource Cards(params CardLink[] links) => new(DiagramKind.Cards, "At a glance",
        Cards: new CardsSpec(
            [
                new CardRow([new Card("one", DiagramIcon.Chip, "One", null, "", CardLook.Highlight)]),
                new CardRow([
                    new Card("a", DiagramIcon.Box, "A", null, "a."), new Card("b", DiagramIcon.Box, "B", null, "b.")
                ])
            ],
            links.Length > 0 ? links : [new CardLink("one", "a"), new CardLink("one", "b")]));

    /// <summary>A flow's narrow form runs down, and drops the columns and the row gap set for the wide one.</summary>
    [Test]
    public void AFlowsNarrowForm_RunsDown_WithNoColumnsAndNoRowGap()
    {
        var flow = NarrowFit.Of(CostPath)!.Flow!;

        Assert.Multiple(() =>
        {
            Assert.That(flow.Direction, Is.EqualTo(FlowDirection.Down));
            Assert.That(flow.Nodes.Select(node => node.Column), Is.All.Null);
            Assert.That(flow.RowGap, Is.Null);
            Assert.That(flow.Nodes, Has.Count.EqualTo(CostPath.Flow!.Nodes.Count));
        });
    }

    /// <summary>A cards picture's narrow form runs down, unless its links leave more than one card.</summary>
    [Test]
    public void ACardsPicturesNarrowForm_RunsDown_UnlessARailLeavesTwoCards()
    {
        var spread = new DiagramSource(DiagramKind.Cards, "Two feed one", Cards: new CardsSpec(
            [
                new CardRow([
                    new Card("x", DiagramIcon.Box, "X", null, "x."), new Card("y", DiagramIcon.Box, "Y", null, "y.")
                ]),
                new CardRow([new Card("z", DiagramIcon.Box, "Z", null, "z.")])
            ],
            [new CardLink("x", "z"), new CardLink("y", "z")]));

        Assert.Multiple(() =>
        {
            Assert.That(NarrowFit.Of(Cards())!.Cards!.Direction, Is.EqualTo(FlowDirection.Down));
            Assert.That(NarrowFit.Of(spread), Is.Null);
            Assert.That(NarrowFit.Has(spread), Is.False);
            Assert.That(NarrowFit.Has(Cards()), Is.True);
        });
    }

    /// <summary>A sequence, a class chart and a ticket have no narrow form yet.</summary>
    [TestCase(DiagramKind.Sequence)]
    [TestCase(DiagramKind.Classes)]
    [TestCase(DiagramKind.Ticket)]
    public void OtherKinds_HaveNoNarrowForm(DiagramKind kind) =>
        Assert.That(NarrowFit.Has(new DiagramSource(kind, "Any", Ticket: new TicketSpec("T", "T", []))),
            Is.False);

    /// <summary>
    /// Drawn for a page in the narrow fit, the cost flow's boxes and lines are laid out no wider than a phone's
    /// words and the picture is taller than wide; in the wide fit, or with none said, it is drawn as it is today.
    /// The picture's head, its title and its type on one line, may still ask a little more (a finding put to Louis).
    /// </summary>
    [Test]
    public void ThePagesCopy_IsDrawnInTheFitAskedFor()
    {
        var narrow = XElement.Parse(DiagramRenderers.RenderForPage(CostPath, PictureFit.Narrow));
        double Size(XElement svg, string name) =>
            double.Parse((string)svg.Attribute(name)!, CultureInfo.InvariantCulture);
        var laid = GraphLayout.Place(NarrowFit.Of(CostPath)!.Flow!);

        Assert.Multiple(() =>
        {
            Assert.That(laid.Width, Is.LessThanOrEqualTo(PageSvg.NarrowLayout));
            Assert.That(Size(narrow, "height"), Is.GreaterThan(Size(narrow, "width")));
            Assert.That(DiagramRenderers.RenderForPage(CostPath, PictureFit.Wide),
                Is.EqualTo(DiagramRenderers.RenderForPage(CostPath)));
            Assert.That(DiagramRenderers.RenderForPage(CostPath),
                Is.EqualTo(PageSvg.Of(DiagramRenderers.Render(CostPath))));
        });
    }
}
