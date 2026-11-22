using System.Globalization;
using System.Xml.Linq;
using LouisAgent.Diagrams.Cards;
using LouisAgent.Diagrams.Cards.Links;
using LouisAgent.Diagrams.Engine.Flow;
using LouisAgent.Diagrams.Engine.Svg;
using LouisAgent.Diagrams.Engine.Svg.Fits;
using LouisAgent.Diagrams.Flow.Spec;
using LouisAgent.Diagrams.Icons;
using LouisAgent.Diagrams.Sequence.Spec;
using LouisAgent.Diagrams.Sequence.Spec.Steps;
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

    /// <summary>One call and its answer between two participants.</summary>
    private static readonly DiagramSource OneCall = new(DiagramKind.Sequence, "One call", Sequence: new SequenceSpec(
        [new Participant("P", "Program"), new Participant("S", "Store")],
        [new MessageStep("P", "S", "Read()"), new ReplyStep("S", "P", "rows")]));

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

    /// <summary>
    /// F169.7: a sequence has a narrow form, and it is the source itself, since its phone drawing is a drawing of its
    /// own and the data needs telling nothing.
    /// </summary>
    [Test]
    public void ASequence_HasANarrowForm_TheSourceItself() => Assert.Multiple(() =>
    {
        Assert.That(NarrowFit.Has(OneCall), Is.True);
        Assert.That(NarrowFit.Of(OneCall), Is.SameAs(OneCall));
    });

    /// <summary>
    /// F169.7: drawn for a page in the narrow fit, a sequence is its steps down a phone's words, 342 wide with no
    /// lifeline; in the wide fit it is drawn as it is today.
    /// </summary>
    [Test]
    public void ASequencesPageCopy_IsItsStepsOnAPhone_AndAsItWasWide()
    {
        var narrow = XElement.Parse(DiagramRenderers.RenderForPage(OneCall, PictureFit.Narrow));
        bool Has(XElement svg, string cls) => svg.Descendants()
            .Any(shape => ((string?)shape.Attribute("class") ?? "").Split(' ').Contains(cls));

        Assert.Multiple(() =>
        {
            Assert.That(double.Parse((string)narrow.Attribute("width")!, CultureInfo.InvariantCulture),
                Is.EqualTo(PageSvg.PhoneWords));
            Assert.That(Has(narrow, "seq-life"), Is.False);
            Assert.That(Has(narrow, "stl-name"), Is.True);
            Assert.That(DiagramRenderers.RenderForPage(OneCall, PictureFit.Wide),
                Is.EqualTo(PageSvg.Of(DiagramRenderers.Render(OneCall))));
        });
    }

    /// <summary>A class chart and a ticket have no narrow form yet.</summary>
    [TestCase(DiagramKind.Classes)]
    [TestCase(DiagramKind.Ticket)]
    public void OtherKinds_HaveNoNarrowForm(DiagramKind kind) =>
        Assert.That(NarrowFit.Has(new DiagramSource(kind, "Any", Ticket: new TicketSpec("T", "T", []))),
            Is.False);

    /// <summary>
    /// Drawn for a page in the narrow fit, the cost flow's boxes and lines are laid out no wider than a phone's
    /// words and the picture is taller than wide; in the wide fit, or with none said, it is drawn as it is today.
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

    /// <summary>
    /// Louis's pick "C: the type under the title" (2026-11-22): on a phone a picture's type stands on its own line
    /// under its title, both at the inset, so a long title and its type never make the picture wider than a phone's
    /// words; the cost flow and a cards picture with a long title are no wider than 342.
    /// </summary>
    [Test]
    public void OnAPhone_TheTypeStandsUnderTheTitle()
    {
        var longTitle = Cards() with { Title = "Agents it already runs, at a glance" };
        var flow = XElement.Parse(DiagramRenderers.RenderForPage(CostPath, PictureFit.Narrow));
        var cards = XElement.Parse(DiagramRenderers.RenderForPage(longTitle, PictureFit.Narrow));
        double Number(XElement shape, string name) =>
            double.Parse((string)shape.Attribute(name)!, CultureInfo.InvariantCulture);
        XElement Text(XElement svg, string cls) =>
            svg.Descendants().First(shape => (string?)shape.Attribute("class") == cls);

        Assert.Multiple(() =>
        {
            Assert.That(Number(flow, "width"), Is.LessThanOrEqualTo(PageSvg.PhoneWords));
            Assert.That(Number(cards, "width"), Is.LessThanOrEqualTo(PageSvg.PhoneWords));
            foreach (var svg in new[] { flow, cards })
            {
                var title = Text(svg, "mrq-title");
                var type = Text(svg, "pic-group");
                Assert.That(Number(type, "x"), Is.EqualTo(Number(title, "x")));
                Assert.That(Number(type, "y"), Is.GreaterThan(Number(title, "y")));
                Assert.That((string?)type.Attribute("text-anchor"), Is.EqualTo("start"));
            }
        });
    }

    /// <summary>The wide drawing keeps its type at the right of the title's line.</summary>
    [Test]
    public void WideTheTypeStaysBesideTheTitle()
    {
        var wide = XElement.Parse(DiagramRenderers.RenderForPage(CostPath));
        var type = wide.Descendants().First(shape => (string?)shape.Attribute("class") == "pic-group");

        Assert.That((string?)type.Attribute("text-anchor"), Is.EqualTo("end"));
    }
}
