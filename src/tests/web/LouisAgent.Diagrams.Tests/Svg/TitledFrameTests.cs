using System.Globalization;
using System.Xml.Linq;
using LouisAgent.Diagrams.Cards;
using LouisAgent.Diagrams.Classes.Spec;
using LouisAgent.Diagrams.Engine.Svg;
using LouisAgent.Diagrams.Flow.Spec;
using LouisAgent.Diagrams.Icons;
using LouisAgent.Diagrams.Sequence.Spec;
using LouisAgent.Diagrams.Sequence.Spec.Steps;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Tests.Svg;

/// <summary>
/// The docs' frame round a picture: the caption as a note bar under the drawing, the drawing itself as it was.
/// Every kind writes its own title and key inside the picture (F134.7 to F134.11), so the note is all the frame
/// adds.
/// </summary>
public class TitledFrameTests
{
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

    /// <summary>
    /// A class chart of two types, a title with a colon and a caption with one.
    /// </summary>
    private static DiagramSource Accounts(string title = "Accounts: the browser's calls to the store") =>
        new(DiagramKind.Classes, title, "Multi-tenant by design: every account lives within its tenant.",
            Classes: new ClassesSpec(
                [new ClassType("browser", "Browser app"), new ClassType("store", "AccountUserStore")],
                [new ClassRelation("browser", "store", RelationKind.Uses)], FlowDirection.Down));

    /// <summary>The same picture as a flow, which has its own title.</summary>
    private static DiagramSource AccountsFlow() =>
        new(DiagramKind.Flow, "Accounts: the browser's calls to the store",
            "Multi-tenant by design: every account lives within its tenant.",
            new GraphSpec(
                [new GraphNode("browser", "Browser app", Tone: DiagramTone.Blue), new GraphNode("store", "Store")],
                [new GraphEdge("browser", "store")], FlowDirection.Down));

    /// <summary>
    /// A flow writes its own title and says in its own key what its tones mean, so the frame gives it no bar and no
    /// legend, only the note bar under it.
    /// </summary>
    [Test]
    public void AFlow_HasItsOwnTitle_SoNoBarAndNoLegend_OnlyTheNote()
    {
        var svg = XDocument.Parse(DiagramRenderers.RenderTitled(AccountsFlow()));

        Assert.Multiple(() =>
        {
            Assert.That(Rects(svg, "titlebar"), Is.Empty);
            Assert.That(svg.Descendants(Svg + "text").Where(text => Class(text) == "legend"), Is.Empty);
            Assert.That(Rects(svg, "notebar"), Has.Count.EqualTo(1));
            Assert.That(svg.Descendants(Svg + "text").Where(text => Class(text) == "mrq-title")
                .Select(text => text.Value), Is.EqualTo(new[] { "Accounts: the browser's calls to the store" }));
        });
    }

    /// <summary>
    /// A cards picture writes its own title too (F134.9), so the frame gives it no bar, only the note bar under
    /// it.
    /// </summary>
    [Test]
    public void ACardsPicture_HasItsOwnTitle_SoNoBar_OnlyTheNote()
    {
        var overview = new DiagramSource(DiagramKind.Cards, "A project at a glance", "One project, one part.",
            Cards: new CardsSpec(
                [new CardRow([new Card("project", DiagramIcon.Chip, "The project", null, "What it is.")])], []));

        var svg = XDocument.Parse(DiagramRenderers.RenderTitled(overview));

        Assert.Multiple(() =>
        {
            Assert.That(Rects(svg, "titlebar"), Is.Empty);
            Assert.That(Rects(svg, "notebar"), Has.Count.EqualTo(1));
            Assert.That(svg.Descendants(Svg + "text").Where(text => Class(text) == "mrq-title")
                .Select(text => text.Value), Is.EqualTo(new[] { "A project at a glance" }));
        });
    }

    /// <summary>
    /// A sequence writes its own title and says in its own key what its tones mean (F134.10), so the frame gives
    /// it no bar and no legend, only the note bar under it.
    /// </summary>
    [Test]
    public void ASequence_HasItsOwnTitleAndKey_SoNoBarAndNoLegend_OnlyTheNote()
    {
        var chat = new DiagramSource(DiagramKind.Sequence, "One chat turn", "Every request is measured.",
            Sequence: new SequenceSpec(
                [new Participant("you", "You", Tone: DiagramTone.Blue), new Participant("api", "The API")],
                [new MessageStep("you", "api", "ask")]));

        var svg = XDocument.Parse(DiagramRenderers.RenderTitled(chat));
        List<string> Words(string cls) =>
            [.. svg.Descendants(Svg + "text").Where(text => Class(text) == cls).Select(text => text.Value)];

        Assert.Multiple(() =>
        {
            Assert.That(Rects(svg, "titlebar"), Is.Empty);
            Assert.That(Rects(svg, "notebar"), Has.Count.EqualTo(1));
            Assert.That(Words("mrq-title"), Is.EqualTo(new[] { "One chat turn" }));
            Assert.That(Words("legend"), Is.Empty, "the frame's legend");
            Assert.That(Words("pic-means"), Is.EqualTo(new[] { "starts it", "a call" }), "the picture's own key");
        });
    }

    /// <summary>
    /// A class chart writes its own title and says in its own key what its tones and its lines mean (F134.11), so
    /// the frame gives it no bar and no legend, only the note bar under it.
    /// </summary>
    [Test]
    public void AClassChart_HasItsOwnTitleAndKey_SoNoBarAndNoLegend_OnlyTheNote()
    {
        var svg = XDocument.Parse(DiagramRenderers.RenderTitled(Accounts()));
        List<string> Words(string cls) =>
            [.. svg.Descendants(Svg + "text").Where(text => Class(text) == cls).Select(text => text.Value)];

        Assert.Multiple(() =>
        {
            Assert.That(Rects(svg, "titlebar"), Is.Empty);
            Assert.That(Rects(svg, "notebar"), Has.Count.EqualTo(1));
            Assert.That(Words("mrq-title"), Is.EqualTo(new[] { "Accounts: the browser's calls to the store" }));
            Assert.That(Words("legend"), Is.Empty, "the frame's legend");
            Assert.That(Words("pic-means"), Is.EqualTo(new[] { "this project", "a class", "uses" }),
                "the picture's own key");
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

    /// <summary>A note without a colon is all plain.</summary>
    [Test]
    public void WithoutAColon_ANoteIsPlain()
    {
        var svg = XDocument.Parse(DiagramRenderers.RenderTitled(Accounts() with
        {
            Caption = "The separation ADR 0027 and ADR 0028 document."
        }));

        // The note's own words, not the brand's mark, which is a span of the wordmark.
        var note = svg.Descendants(Svg + "text").Where(text => Class(text) == "notes").Descendants(Svg + "tspan");
        Assert.That(note.Select(Class), Is.All.EqualTo("note-rest"));
    }

    /// <summary>
    /// The drawing stays where it was, whole, with the note under it: the framed picture is taller by the note
    /// and no wider.
    /// </summary>
    [Test]
    public void TheDrawing_StaysAsItWas_WithTheNoteUnderIt()
    {
        var plain = XDocument.Parse(DiagramRenderers.Render(Accounts() with { Caption = null }));
        var framed = XDocument.Parse(DiagramRenderers.RenderTitled(Accounts()));

        var drawing = framed.Descendants(Svg + "g").Single(g => Class(g) == "drawing");
        var note = Rects(framed, "notebar").Single();
        Assert.Multiple(() =>
        {
            Assert.That(drawing.Attribute("transform")!.Value, Is.EqualTo("translate(0 0)"));
            Assert.That(Rects(drawing, "mrq-glass").Select(card => card.ToString()),
                Is.Not.Empty.And.EqualTo(Rects(plain, "mrq-glass").Select(card => card.ToString())));
            Assert.That(Number(framed.Root!, "width"), Is.EqualTo(Number(plain.Root!, "width")));
            Assert.That(Number(framed.Root!, "height"),
                Is.EqualTo(Number(plain.Root!, "height") + 16 + Number(note, "height")).Within(0.11));
        });
    }

    /// <summary>A long caption wraps inside the drawing's width, however narrow the drawing is.</summary>
    [Test]
    public void ALongCaption_WrapsInsideTheDrawing()
    {
        var svg = XDocument.Parse(DiagramRenderers.RenderTitled(Accounts() with
        {
            Caption = "A rather long note: one that a narrow drawing of two types could never hold on a single " +
                      "line, however it is set, so it goes over several"
        }));

        Assert.That(svg.Descendants(Svg + "text").Count(text => Class(text) == "notes"), Is.GreaterThan(1));
    }

    /// <summary>
    /// A picture with no caption gets no note, and a picture drawn without the frame has none either.
    /// </summary>
    [Test]
    public void WithoutACaption_OrWithoutTheFrame_APictureHasNoNote() => Assert.Multiple(() =>
    {
        var bare = Accounts() with { Caption = null };
        Assert.That(Rects(XDocument.Parse(DiagramRenderers.RenderTitled(bare)), "notebar"), Is.Empty);
        Assert.That(Rects(XDocument.Parse(DiagramRenderers.Render(Accounts())), "notebar"), Is.Empty);
    });

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
