using System.Xml.Linq;
using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.Flows;
using LouisAgent.Diagrams.Sources;

namespace LouisAgent.Diagrams.Tests.Svg.Renderers.Parts.Flows;

/// <summary>
/// A flow drawn in the new look (F134.8), which since F134.12 is the others': its title as one small line over a
/// soft light, boxes as plain panels in their shape, each arrow at its own point with dots that march to its
/// head, a ring where a box's line leaves, and the key that says how to read it. The drawing decides no place
/// itself, so these tests are about what is drawn and how it is marked.
/// </summary>
public class FlowDrawingTests
{
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

    /// <summary>
    /// A small procedure: an error starts it, a question sends it two ways, and one way reads a runbook. It has a
    /// group, a word on an arrow, a dashed arrow, and one of each shape.
    /// </summary>
    private const string Procedure = """
                                     {
                                       "kind": "flow",
                                       "type": "activity",
                                       "title": "Runbooks: how one is used",
                                       "flow": {
                                         "nodes": [
                                           { "id": "error", "label": "An error", "icon": "message", "tone": "blue",
                                             "shape": "terminal" },
                                           { "id": "known", "label": "Runbook for\nthis method?", "shape": "decision" },
                                           { "id": "team", "label": "Tell the team", "icon": "user", "tone": "red" },
                                           { "id": "sort", "label": "Sort it", "icon": "book", "tone": "teal",
                                             "group": "triage" },
                                           { "id": "book", "label": "The runbook", "icon": "database", "tone": "amber",
                                             "shape": "data", "group": "triage" }
                                         ],
                                         "edges": [
                                           { "from": "error", "to": "known" },
                                           { "from": "known", "to": "team", "word": "no" },
                                           { "from": "known", "to": "sort", "word": "yes" },
                                           { "from": "sort", "to": "book", "style": "dashed" }
                                         ],
                                         "groups": [ { "id": "triage", "label": "Triage" } ]
                                       }
                                     }
                                     """;

    /// <summary>The procedure as the new drawing draws it.</summary>
    private static string Drawn() => FlowDrawing.Of(DiagramReader.Read(Procedure), Geometries.InUse);

    /// <summary>Every shape of the drawing that has a class, with that class.</summary>
    private static List<(XElement Shape, string Class)> Shapes() =>
        [.. XElement.Parse(Drawn()).Descendants().Where(shape => shape.Attribute("class") is not null)
            .Select(shape => (shape, (string)shape.Attribute("class")!))];

    /// <summary>The words drawn in a class.</summary>
    private static List<string> Words(string cls) =>
        [.. Shapes().Where(shape => shape.Class.Split(' ').Contains(cls)).Select(shape => shape.Shape.Value)];

    /// <summary>The whole picture, byte for byte, so a change to the look is a change someone reads.</summary>
    [Test]
    public void TheProcedure_IsDrawnAsItsGolden() => Golden.Match("flow-look.svg", Drawn());

    /// <summary>
    /// The picture's title is one small line, the whole title as it is written, in the face the other kinds'
    /// titles have. Behind the top of the picture stands the soft light, and the ground has no dots.
    /// </summary>
    [Test]
    public void TheTitle_IsOneSmallLine_OverTheLight()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Words("mrq-title"), Is.EqualTo(new[] { "Runbooks: how one is used" }));
            Assert.That(Words("pic-group"), Has.None.EqualTo("how one is used"));
            Assert.That(Shapes().Count(shape => shape.Class == "mrq-light"), Is.EqualTo(1));
            Assert.That(Shapes().Select(shape => shape.Class), Has.None.EqualTo("pic-dots"));
        });
    }

    /// <summary>Every word of the JSON is in the picture: a label's lines, a group's label, an arrow's word.</summary>
    [TestCase("An error")]
    [TestCase("Runbook for")]
    [TestCase("this method?")]
    [TestCase("The runbook")]
    [TestCase("Triage")]
    [TestCase("yes")]
    [TestCase("How to read it")]
    [TestCase("a decision")]
    [TestCase("outside the process")]
    public void EveryWord_IsInThePicture(string word)
    {
        Assert.That(Shapes().Select(shape => shape.Shape.Value), Has.Some.EqualTo(word));
    }

    /// <summary>The flow itself, without the title above it and the key under it.</summary>
    private static XElement Flow() => XElement.Parse(Drawn()).Descendants(Svg + "g")
        .Single(group => (string?)group.Attribute("class") == "flow");

    /// <summary>
    /// A plain box is a panel and nothing more: no tone on it, since its badge says its tone. A group's frame is
    /// a panel too (F134.12), so a box in a group is a step deeper by itself. A shape that means something is
    /// marked as one and wears its tone, so the look can outline it: here the start, the question and the data.
    /// </summary>
    [Test]
    public void EachBox_IsAPanel_AndAShapeIsMarkedAsOne()
    {
        var panels = Flow().Descendants().Select(shape => (Shape: shape, Class: (string?)shape.Attribute("class")))
            .Where(shape => shape.Class?.Split(' ')[0] == "pic-panel")
            .Select(shape => (shape.Shape, Class: shape.Class!)).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(panels, Has.Count.EqualTo(6), "five boxes and the group's frame");
            Assert.That(panels.Count(panel => panel.Class == "pic-panel"), Is.EqualTo(3),
                "the two plain boxes and the group's frame");
            Assert.That(Shapes().Select(shape => shape.Class), Has.None.EqualTo("flow-group"));
            Assert.That(panels.Count(panel => panel.Class.Contains("flow-shape")), Is.EqualTo(3));
            Assert.That(panels.Select(panel => panel.Class), Has.Some.Contains("tone-amber"));
            Assert.That(panels.Count(panel => panel.Shape.Name == Svg + "path"), Is.EqualTo(2),
                "the decision and the slanted box are drawn as paths");
        });
    }

    /// <summary>
    /// A box has no highlight along its top and no marker bar at its left (F134.12): its badge is its only mark.
    /// A decision keeps the short bar over its words, which is wider than it is tall.
    /// </summary>
    [Test]
    public void ABox_HasNoHighlight_AndNoMarkerBar()
    {
        var flow = XElement.Parse(Drawn()).Descendants(Svg + "g")
            .Single(group => (string?)group.Attribute("class") == "flow");
        var bars = flow.Descendants(Svg + "rect")
            .Where(bar => ((string?)bar.Attribute("class") ?? "").StartsWith("pic-fill"))
            .Select(bar => (string?)bar.Attribute("width")).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(Shapes().Select(shape => shape.Class), Has.None.EqualTo("pic-sheen"));
            Assert.That(bars, Has.None.EqualTo("4"), "no marker bar");
            Assert.That(bars.Count(width => width == "28"), Is.EqualTo(4), "a badge a box with an icon");
            Assert.That(bars.Count(width => width == "22"), Is.EqualTo(1), "the decision's bar");
        });
    }

    /// <summary>
    /// The key stands under a faint rule as wide as the picture, as the other kinds' keys do (F134.12, Louis: "do
    /// not look like the other graphs"). A tone's sample is a small badge filled with the tone, since a box's
    /// badge is what says its tone, and what an item means is written in the page's ink.
    /// </summary>
    [Test]
    public void TheKey_StandsUnderARule_AndATonesSampleIsASmallBadge()
    {
        var shapes = Shapes();
        var badges = shapes.Where(shape => (string?)shape.Shape.Attribute("width") == "18").ToList();

        Assert.Multiple(() =>
        {
            Assert.That(shapes.Count(shape => shape.Class == "mrq-rule"), Is.EqualTo(2), "the header's and the key's");
            Assert.That(badges, Has.Count.EqualTo(4), "blue, teal, red and amber");
            Assert.That(badges.Select(badge => badge.Class), Is.All.StartsWith("pic-fill tone-"));
            Assert.That(shapes.Select(shape => shape.Class), Has.None.Contains("pic-swatch"));
            Assert.That(Words("pic-means"), Does.Contain("a decision").And.Contain("starts it"));
        });
    }

    /// <summary>
    /// A box with an icon wears it in a solid badge. A decision has no icon: its shape already says what it is.
    /// </summary>
    [Test]
    public void ABoxWithAnIcon_HasABadge_AndADiamondHasNone()
    {
        Assert.That(Shapes().Count(shape => shape.Class == "pic-badge-icon"), Is.EqualTo(4));
    }

    /// <summary>
    /// A solid arrow has dots that march to its head. A dashed one is drawn dashed and has none.
    /// </summary>
    [Test]
    public void ASolidArrow_Marches_AndADashedOneDoesNot()
    {
        var lines = Shapes().Where(shape => shape.Class.StartsWith("pic-line ")).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(lines, Has.Count.EqualTo(4));
            Assert.That(lines.Count(line => line.Class.Contains("reads")), Is.EqualTo(1), "the dashed one");
            Assert.That(Shapes().Count(shape => shape.Class == "pic-march"), Is.EqualTo(3));
        });
    }

    /// <summary>
    /// A ring marks where a box's line leaves it, once for a box, though the question's line branches two ways.
    /// </summary>
    [Test]
    public void ABoxWithArrowsOut_HasOneRing()
    {
        Assert.That(Shapes().Count(shape => shape.Class.StartsWith("pic-port ")), Is.EqualTo(3));
    }

    /// <summary>
    /// The picture carries the shared look and the flow's own, both its light and its dark colours, and stops the
    /// marching dots for a reader who asked for less motion.
    /// </summary>
    [Test]
    public void ThePicture_CarriesTheLook_BothThemes_AndStopsOnRequest()
    {
        string drawn = Drawn();

        Assert.Multiple(() =>
        {
            Assert.That(drawn, Does.Contain(".pic-panel").And.Contain(".flow-shape"));
            Assert.That(drawn, Does.Contain("@media (prefers-color-scheme: dark)"));
            Assert.That(drawn, Does.Contain(".pic-march { display: none; }"));
            Assert.That(drawn, Does.Contain("id=\"arrow-blue\""), "a toned arrow's head is in the picture");
        });
    }
}
