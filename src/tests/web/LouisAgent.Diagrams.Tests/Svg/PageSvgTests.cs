using System.Globalization;
using System.Xml.Linq;
using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Svg;
using LouisAgent.Diagrams.Engine.Svg.Paints;
using LouisAgent.Diagrams.Engine.Svg.Theme;
using LouisAgent.Diagrams.Flow.Spec;
using LouisAgent.Diagrams.Icons;
using LouisAgent.Diagrams.Sources;
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

    /// <summary>
    /// The page's copy is trimmed to the drawing (F127.4.1, Louis's pick on the phone check): no empty ground around
    /// it, only room for its outline, so a phone's narrow chat shrinks the words less.
    /// </summary>
    [Test]
    public void ThePagesCopy_IsTrimmedToTheDrawing()
    {
        var file = XElement.Parse(DiagramRenderers.Render(Ticket()));
        var page = XElement.Parse(DiagramRenderers.RenderForPage(Ticket()));
        double fileWidth = double.Parse((string)file.Attribute("width")!, CultureInfo.InvariantCulture);

        Assert.Multiple(() =>
        {
            Assert.That((string?)page.Attribute("viewBox"), Does.StartWith("-2 -2 "));
            Assert.That(double.Parse((string)page.Attribute("width")!, CultureInfo.InvariantCulture),
                Is.EqualTo(fileWidth - 44));
            Assert.That(page.Elements(Svg + "rect").Where(rect => (string?)rect.Attribute("class") == "ground"),
                Is.Empty);
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

    /// <summary>
    /// A drawing with paints and no arrow keeps its paints in the page's copy, since its shapes are filled with
    /// them, and still brings no arrowhead.
    /// </summary>
    [Test]
    public void ADrawingWithPaintsAndNoArrow_KeepsItsPaints_AndBringsNoArrowhead()
    {
        var box = Geometries.InUse.Box(0, 0, 10, 10);
        string file = new SvgWriter().Paint(new Fade("fade", PaintRun.Across, 0, 1)).Box(box, 2, "filled")
            .Write(SvgTheme.Site, "A picture", 100, 100);

        var defined = XElement.Parse(PageSvg.Of(file)).Elements(Svg + "defs").Elements().ToList();

        Assert.That(defined.Select(element => element.Name.LocalName), Is.EqualTo(new[] { "linearGradient" }));
    }

    /// <summary>The file for the docs stays as it was: its styles plain, read by any viewer.</summary>
    [Test]
    public void TheFile_KeepsItsPlainStyles() =>
        Assert.That(DiagramRenderers.Render(Ticket()), Does.Not.Contain("@scope"));

    /// <summary>
    /// F164.1: in a page, a drawing wears the page's theme as well as its style (Louis's pick C: "a palette per theme
    /// in the engine"): flat's own colours on a flat page, glass's on a glass one, light or dark.
    /// </summary>
    [Test]
    public void ThePagesCopy_WearsFlatOrGlass()
    {
        string page = DiagramRenderers.RenderForPage(Flow());

        Assert.Multiple(() =>
        {
            Assert.That(page, Does.Contain("[data-theme=\"flat\"][data-style=\"light\"] :scope"));
            Assert.That(page, Does.Contain("[data-theme=\"flat\"][data-style=\"dark\"] :scope"));
            Assert.That(page, Does.Contain("[data-theme=\"glass\"][data-style=\"light\"] :scope"));
            Assert.That(page, Does.Contain("--ground: #f7f7f8").And.Contain("--ground: #0c0c0e"),
                "flat's grounds, a tad darker than white and a very dark grey, as its pages are");
        });
    }

    /// <summary>
    /// On a page, only the cards are filled (Louis: "can we make all backgrounds transparent only the cards can have a
    /// background colour, light is smoke white"): no light and no sheen behind the picture, and every card, glass's and
    /// the quiet one alike, its own fill under its line, smoke white in the light.
    /// </summary>
    [Test]
    public void OnAPage_OnlyTheCardsAreFilled()
    {
        string page = DiagramRenderers.RenderForPage(Flow());

        Assert.Multiple(() =>
        {
            Assert.That(page, Does.Contain(":scope .mrq-glass, :scope .crd-quiet { fill: var(--pic-body)"));
            Assert.That(page, Does.Contain("--mrq-glow: 0; --mrq-glow-far: 0"), "no light behind the picture");
            Assert.That(page, Does.Contain("--pic-body: #f5f5f5"), "smoke white in the light");
        });
    }

    /// <summary>
    /// F169.1: on a flat page a card's line is flat's own line, the firmer grey in the dark, so a picture's cards stand
    /// out from the page as much as the page's own panes do (Louis's pick: pictures take the firmer line too).
    /// </summary>
    [Test]
    public void OnAFlatPage_ACardsLine_IsFlatsOwnLine()
    {
        string page = DiagramRenderers.RenderForPage(Flow());

        Assert.Multiple(() =>
        {
            Assert.That(page, Does.Contain("--line: #404048"), "the dark picture's own line");
            Assert.That(page, Does.Contain("--pic-hair: #404048; --mrq-hair: #404048"), "a card's line in the dark");
            Assert.That(page, Does.Contain("--pic-hair: #a7aaba; --mrq-hair: #a7aaba"), "a card's line in the light");
        });
    }

    /// <summary>A file names no site's theme: the docs' pictures stay byte for byte as they were.</summary>
    [Test]
    public void TheFile_WearsNoSitesTheme() =>
        Assert.That(DiagramRenderers.Render(Flow()), Does.Not.Contain("data-theme"));

    /// <summary>The parts of a picture that are a glow or a light, which a flat page hides.</summary>
    private const string Lights =
        ":is(.crd-rail-glow, .seq-beam-glow, .mrq-light, .mrq-edge, .ticket-light) { display: none; }";

    /// <summary>
    /// F169.4: on a flat page a picture has no glow and no light (Louis's pick 3, "A: no fade, no blur, the six colours
    /// kept"): the glow behind a rail and a beam, the light behind the top, a panel's edge of light and a ticket's
    /// light are hidden, in light and dark alike, and only on a flat page. The file for the docs holds none of it.
    /// </summary>
    [Test]
    public void OnAFlatPage_APicture_HasNoGlowAndNoLight()
    {
        string page = DiagramRenderers.RenderForPage(Flow());

        Assert.Multiple(() =>
        {
            Assert.That(page, Does.Contain($"[data-theme=\"flat\"] :scope {Lights}"));
            Assert.That(page, Does.Not.Contain($"[data-theme=\"glass\"] :scope {Lights}"));
            Assert.That(DiagramRenderers.Render(Flow()), Does.Not.Contain(Lights));
        });
    }
}
