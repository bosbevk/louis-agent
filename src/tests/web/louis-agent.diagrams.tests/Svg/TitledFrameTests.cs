using System.Globalization;
using System.Xml.Linq;
using LouisAgent.Diagrams.Flow.Spec;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Svg;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Tests.Svg;

/// <summary>
/// The docs' frame round a picture (CL60.2, Louis's pick "Both looks"): a navy title bar on top, the title's part
/// before its colon in bold, and the caption as a note bar under the drawing, the drawing itself as it was.
/// </summary>
public class TitledFrameTests
{
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

    /// <summary>Two boxes, a title with a colon and a caption with one.</summary>
    private static DiagramSource Accounts(string title = "Accounts: the browser's calls to the store") =>
        new(DiagramKind.Flow, title, "Multi-tenant by design: every account lives within its tenant.",
            new GraphSpec([new GraphNode("browser", "Browser app"), new GraphNode("store", "AccountUserStore")],
                [new GraphEdge("browser", "store")], FlowDirection.Down));

    /// <summary>The title stands in a bar on top, its part before the colon in bold.</summary>
    [Test]
    public void TheTitle_StandsInABar_ItsHeadInBold()
    {
        var svg = XDocument.Parse(DiagramRenderers.RenderTitled(Accounts()));

        var spans = svg.Descendants(Svg + "tspan").ToList();
        Assert.Multiple(() =>
        {
            Assert.That(Rects(svg, "titlebar"), Has.Count.EqualTo(1));
            Assert.That(spans.Where(span => Class(span) == "title-head").Select(span => span.Value),
                Is.EqualTo(new[] { "Accounts:" }));
            Assert.That(string.Join(' ', spans.Where(span => Class(span) == "title-rest").Select(span => span.Value)),
                Is.EqualTo("the browser's calls to the store"));
        });
    }

    /// <summary>
    /// The caption stands in a note bar under the drawing, its lead in bold; no plain caption is left.
    /// </summary>
    [Test]
    public void TheCaption_StandsInANoteBar()
    {
        var svg = XDocument.Parse(DiagramRenderers.RenderTitled(Accounts()));

        var spans = svg.Descendants(Svg + "tspan").ToList();
        Assert.Multiple(() =>
        {
            Assert.That(Rects(svg, "notebar"), Has.Count.EqualTo(1));
            Assert.That(string.Join(' ', spans.Where(span => Class(span) == "note-head").Select(span => span.Value)),
                Is.EqualTo("Multi-tenant by design:"));
            Assert.That(svg.Descendants(Svg + "text").Count(text => Class(text) == "caption"), Is.Zero);
        });
    }

    /// <summary>
    /// A title without a colon is all bold, as a card overview's is; a note without one is all plain.
    /// </summary>
    [Test]
    public void WithoutAColon_ATitleIsBold_AndANotePlain()
    {
        var svg = XDocument.Parse(DiagramRenderers.RenderTitled(Accounts("Engine, identities and packs") with
        {
            Caption = "The separation ADR 0027 and ADR 0028 document."
        }));

        var spans = svg.Descendants(Svg + "tspan").Select(Class).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(spans.Where(cls => cls!.StartsWith("title", StringComparison.Ordinal)),
                Is.All.EqualTo("title-head"));
            Assert.That(spans.Where(cls => cls!.StartsWith("note", StringComparison.Ordinal)),
                Is.All.EqualTo("note-rest"));
        });
    }

    /// <summary>The drawing moves down under the bar, every box the same as the untitled picture's.</summary>
    [Test]
    public void TheDrawing_MovesDownUnderTheBar()
    {
        var plain = XDocument.Parse(DiagramRenderers.Render(Accounts() with { Caption = null }));
        var titled = XDocument.Parse(DiagramRenderers.RenderTitled(Accounts()));

        var drawing = titled.Descendants(Svg + "g").Single(g => Class(g) == "drawing");
        double bar = Number(Rects(titled, "titlebar").Single(), "height");
        Assert.Multiple(() =>
        {
            Assert.That(drawing.Attribute("transform")!.Value, Does.StartWith("translate(0 "));
            Assert.That(Number(drawing, "transform"), Is.GreaterThan(bar));
            Assert.That(Rects(drawing, "box").Select(rect => rect.ToString()),
                Is.EqualTo(Rects(plain, "box").Select(rect => rect.ToString())));
        });
    }

    /// <summary>A long title wraps inside the drawing's width, however narrow the drawing is.</summary>
    [Test]
    public void ALongTitle_WrapsInsideTheDrawing()
    {
        var svg = XDocument.Parse(DiagramRenderers.RenderTitled(Accounts(
            "A rather long title: one that a narrow drawing of two boxes could never hold on a single line")));

        var bar = Rects(svg, "titlebar").Single();
        var lines = svg.Descendants(Svg + "text").Where(text => Class(text) == "heading").ToList();
        Assert.Multiple(() =>
        {
            Assert.That(lines, Has.Count.GreaterThan(1));
            foreach (var line in lines)
                Assert.That(Number(line, "x") + line.Value.Length * TitledFrame.TitleChar,
                    Is.LessThanOrEqualTo(Number(bar, "x") + Number(bar, "width")));
        });
    }

    /// <summary>
    /// A chart in colours gets a legend under its drawing (Louis: "can we have a legend for the color"): one swatch
    /// for each tone it uses, in the tones' order, each with what the tone means; a chart without colours has none.
    /// </summary>
    [Test]
    public void AToneChart_HasALegend_OfItsTones()
    {
        var toned = Accounts() with
        {
            Flow = new GraphSpec(
                [
                    new GraphNode("store", "AccountUserStore", Tone: DiagramTone.Teal),
                    new GraphNode("browser", "Browser app", Tone: DiagramTone.Blue),
                    new GraphNode("mail", "IEmailSender", Tone: DiagramTone.Red),
                    new GraphNode("tenant", "UseTenant", Tone: DiagramTone.Blue)
                ],
                [new GraphEdge("browser", "store")], FlowDirection.Down)
        };

        var svg = XDocument.Parse(DiagramRenderers.RenderTitled(toned));
        var plain = XDocument.Parse(DiagramRenderers.RenderTitled(Accounts()));

        Assert.Multiple(() =>
        {
            Assert.That(svg.Descendants(Svg + "rect").Select(Class).Where(cls => cls!.StartsWith("swatch")),
                Is.EqualTo(new[] { "swatch tone-blue", "swatch tone-teal", "swatch tone-red" }));
            Assert.That(svg.Descendants(Svg + "text").Where(text => Class(text) == "legend").Select(text => text.Value),
                Is.EqualTo(new[] { "starts it", "contracts and state", "outside the process" }));
            Assert.That(plain.Descendants(Svg + "rect").Select(Class).Where(cls => cls!.StartsWith("swatch")),
                Is.Empty);
        });
    }

    /// <summary>A picture drawn without the frame has no bar, as the chat's tool cards will draw it.</summary>
    [Test]
    public void AnUntitledPicture_HasNoBar() =>
        Assert.That(Rects(XDocument.Parse(DiagramRenderers.Render(Accounts())), "titlebar"), Is.Empty);

    /// <summary>The rectangles of a class under an element.</summary>
    private static List<XElement> Rects(XContainer under, string cls) =>
        under.Descendants(Svg + "rect").Where(rect => Class(rect) == cls).ToList();

    /// <summary>An element's class.</summary>
    private static string? Class(XElement element) => (string?)element.Attribute("class");

    /// <summary>The first number in an attribute: a length, or a translate's y.</summary>
    private static double Number(XElement element, string attribute)
    {
        string value = element.Attribute(attribute)!.Value;
        string number = value.StartsWith("translate", StringComparison.Ordinal)
            ? value.Split(' ')[1].TrimEnd(')')
            : value;
        return double.Parse(number, CultureInfo.InvariantCulture);
    }
}
