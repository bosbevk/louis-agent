using System.Xml.Linq;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Svg.Renderers.Parts;

namespace LouisAgent.Diagrams.Tests.Svg.Renderers.Parts;

/// <summary>
/// A flow drawn in the new look (F134.8): its own title, boxes as tinted panels in their shape, each arrow at its
/// own point with dots that march to its head, a ring where a box's line leaves, and the key that says how to read
/// it. The drawing decides no place itself, so these tests are about what is drawn and how it is marked.
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
    private static string Drawn() => FlowDrawing.Of(DiagramReader.Read(Procedure));

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
    /// The picture has its own title: the words before the colon, and the rest in the smaller line under it.
    /// </summary>
    [Test]
    public void TheTitle_IsItsWordsBeforeTheColon_AndTheRestUnderIt()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Words("pic-title"), Is.EqualTo(new[] { "Runbooks" }));
            Assert.That(Words("pic-group"), Does.Contain("how one is used"));
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

    /// <summary>
    /// A box is a panel in its tone. A shape that means something is marked as one, so the look can make it stand
    /// out: here the start, the question and the data, and not the two plain boxes.
    /// </summary>
    [Test]
    public void EachBox_IsAPanel_AndAShapeIsMarkedAsOne()
    {
        var panels = Shapes().Where(shape => shape.Class.StartsWith("pic-panel ")).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(panels, Has.Count.EqualTo(5));
            Assert.That(panels.Count(panel => panel.Class.Contains("flow-shape")), Is.EqualTo(3));
            Assert.That(panels.Select(panel => panel.Class), Has.Some.Contains("tone-amber"));
            Assert.That(panels.Count(panel => panel.Shape.Name == Svg + "path"), Is.EqualTo(2),
                "the diamond and the slanted box are drawn as paths");
        });
    }

    /// <summary>
    /// A box with an icon wears it in a solid badge. A diamond has no icon: its shape already says what it is.
    /// </summary>
    [Test]
    public void ABoxWithAnIcon_HasABadge_AndADiamondHasNone()
    {
        Assert.That(Shapes().Count(shape => shape.Class == "flow-icon"), Is.EqualTo(4));
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
