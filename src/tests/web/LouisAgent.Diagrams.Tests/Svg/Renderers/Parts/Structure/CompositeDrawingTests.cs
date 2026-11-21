using System.Globalization;
using System.Xml.Linq;
using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Svg;
using LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.Structure;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Tests.Composite;

namespace LouisAgent.Diagrams.Tests.Svg.Renderers.Parts.Structure;

/// <summary>
/// A composite structure diagram drawn (F134.13, Louis's pick "Pins"): one class as a frame, its parts inside it,
/// a pin on the frame's edge for each port, a plain line between two parts that work together, and a line that
/// runs for what is passed on. The drawing decides one place itself, the frame, so these tests are about what is
/// drawn, how it is marked, and where the frame's edges stand.
/// </summary>
public class CompositeDrawingTests
{
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

    /// <summary>The shared sample as the drawing draws it.</summary>
    private static string Drawn() =>
        CompositeDrawing.Of(DiagramReader.Read(CompositeSamples.Engine), Geometries.InUse);

    /// <summary>The class itself: the frame and everything in and on it, without the title and the key.</summary>
    private static XElement Class() => XElement.Parse(Drawn()).Descendants(Svg + "g")
        .Single(group => (string?)group.Attribute("class") == "cps");

    /// <summary>The shapes of the class that have a class, with that class.</summary>
    private static List<(XElement Shape, string Class)> Shapes() =>
    [
        .. Class().Descendants().Where(shape => shape.Attribute("class") is not null)
            .Select(shape => (shape, (string)shape.Attribute("class")!))
    ];

    /// <summary>A shape's number, as the SVG writes it.</summary>
    private static double Number(XElement shape, string name) =>
        double.Parse((string)shape.Attribute(name)!, CultureInfo.InvariantCulture);

    /// <summary>The whole picture, byte for byte, so a change to the look is a change someone reads.</summary>
    [Test]
    public void TheEngine_IsDrawnAsItsGolden() => Golden.Match("composite-look.svg", Drawn());

    /// <summary>
    /// Every word of the JSON is in the picture, and so are what type of diagram it is and the key's own words.
    /// </summary>
    [TestCase("The engine: what it is made of")]
    [TestCase("Composite structure diagram")]
    [TestCase("AgentEngine")]
    [TestCase("class")]
    [TestCase("LouisAgent.Core, Agent")]
    [TestCase("TurnRunner")]
    [TestCase("turns")]
    [TestCase("one turn on the pipeline")]
    [TestCase("ToolRegistry")]
    [TestCase("prompts")]
    [TestCase("ProcessPromptAsync")]
    [TestCase("model")]
    [TestCase("IChatClient")]
    [TestCase("the pipeline")]
    [TestCase("How to read it")]
    [TestCase("asked of the class")]
    [TestCase("what the class needs")]
    [TestCase("two parts work together")]
    [TestCase("passed on")]
    public void EveryWord_IsInThePicture(string word)
    {
        var words = XElement.Parse(Drawn()).Descendants(Svg + "text").Select(text => text.Value);

        Assert.That(words, Has.Some.EqualTo(word));
    }

    /// <summary>
    /// The class is a panel, and so is each of its parts: five panels for the frame and four parts. A port has no
    /// panel: it is a pin.
    /// </summary>
    [Test]
    public void TheClassAndEachPart_IsAPanel_AndAPortIsAPin()
    {
        var shapes = Shapes();

        Assert.Multiple(() =>
        {
            Assert.That(shapes.Count(shape => shape.Class == "pic-panel"), Is.EqualTo(5));
            Assert.That(shapes.Count(shape => shape.Class.StartsWith("cmp-pin ")), Is.EqualTo(3), "a pin a port");
            Assert.That(shapes.Count(shape => shape.Class.StartsWith("cmp-pin ") && shape.Class.EndsWith(" up")),
                Is.EqualTo(1), "up for what the class needs, down for what is asked of it");
            Assert.That(shapes.Where(shape => shape.Class.StartsWith("cmp-pin ") && shape.Class.EndsWith(" up"))
                .Select(shape => shape.Class), Has.All.Contains("tone-teal"), "a port wears its own tone");
            Assert.That(shapes.Where(shape => shape.Class.StartsWith("cmp-pin ") && !shape.Class.EndsWith(" up"))
                .Select(shape => shape.Class), Has.All.Contains("tone-green"), "or the class's, when it has none");
        });
    }

    /// <summary>
    /// A line between two parts is plain: no arrowhead, and no dots. What is passed on, in from a pin or out to
    /// one, has an arrowhead and the dots that run along a flow's line.
    /// </summary>
    [Test]
    public void ALineBetweenTwoParts_IsPlain_AndWhatIsPassedOnRuns()
    {
        var shapes = Shapes();
        var plain = shapes.Where(shape => shape.Class == "cps-wire").ToList();
        var passed = shapes.Where(shape => shape.Class.StartsWith("pic-line ")).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(plain, Has.Count.EqualTo(3), "a line a connector");
            Assert.That(plain.Select(line => line.Shape.Attribute("marker-end")), Is.All.Null);
            Assert.That(passed, Has.Count.EqualTo(3), "a line a port");
            Assert.That(passed.Select(line => line.Shape.Attribute("marker-end")), Is.All.Not.Null);
            Assert.That(shapes.Count(shape => shape.Class == "pic-march"), Is.EqualTo(3), "dots on what is passed on");
        });
    }

    /// <summary>
    /// A line's word stands in its pill over every line (Louis, of a line drawn across "the pipeline": "line
    /// issue"). Two lines that leave one part share their first stretch, so every line is drawn before any word.
    /// </summary>
    [Test]
    public void ALinesWord_StandsOverEveryLine()
    {
        var order = Shapes().Select(shape => shape.Class).ToList();
        int lastLine = order.FindLastIndex(cls => cls == "cps-wire" || cls == "pic-march");
        int firstWord = order.FindIndex(cls => cls.StartsWith("pic-pill"));

        Assert.Multiple(() =>
        {
            Assert.That(firstWord, Is.GreaterThan(-1), "the pipeline has its pill");
            Assert.That(lastLine, Is.LessThan(firstWord), "no line is drawn after a word's pill");
        });
    }

    /// <summary>
    /// The frame goes round the parts and not round the ports: its left edge runs through the middle of the pins
    /// of what is asked, and its right edge through the middle of the pins of what is needed.
    /// </summary>
    [Test]
    public void TheFramesEdges_RunThroughTheMiddleOfThePins()
    {
        var shapes = Shapes();
        var frame = shapes.First(shape => shape.Class == "pic-panel").Shape;
        double left = Number(frame, "x"), right = left + Number(frame, "width");
        var down = shapes.Where(shape => shape.Class.StartsWith("cmp-pin ") && !shape.Class.EndsWith(" up"));
        var up = shapes.Where(shape => shape.Class.StartsWith("cmp-pin ") && shape.Class.EndsWith(" up"));

        Assert.Multiple(() =>
        {
            Assert.That(down.Select(pin => Number(pin.Shape, "x") + Number(pin.Shape, "width") / 2),
                Is.All.EqualTo(left).Within(0.11));
            Assert.That(up.Select(pin => Number(pin.Shape, "x") + Number(pin.Shape, "width") / 2),
                Is.All.EqualTo(right).Within(0.11));
            Assert.That(shapes.Where(shape => shape.Class == "pic-panel").Skip(1)
                .Select(part => Number(part.Shape, "x")), Is.All.GreaterThan(left).And.All.LessThan(right));
        });
    }

    /// <summary>
    /// The picture carries the looks it wears, both its light and its dark colours, and stops the running dots for
    /// a reader who asked for less motion.
    /// </summary>
    [Test]
    public void ThePicture_CarriesItsLooks_BothThemes_AndStopsOnRequest()
    {
        string drawn = Drawn();

        Assert.Multiple(() =>
        {
            Assert.That(drawn, Does.Contain(".pic-panel").And.Contain(".cmp-pin").And.Contain(".cps-wire"));
            Assert.That(drawn, Does.Contain("@media (prefers-color-scheme: dark)"));
            Assert.That(drawn, Does.Contain(".pic-march { display: none; }"));
        });
    }

    /// <summary>
    /// The kind has its renderer: any picture of the kind is what this drawing draws, and the docs' frame gives
    /// it its note bar and no title bar.
    /// </summary>
    [Test]
    public void AComposite_IsDrawnByItsRenderer_AndFramedForTheDocs()
    {
        var source = DiagramReader.Read(CompositeSamples.Engine);
        var classes = XElement.Parse(DiagramRenderers.RenderTitled(source)).Descendants()
            .Select(shape => (string?)shape.Attribute("class")).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(DiagramRenderers.Render(source),
                Is.EqualTo(CompositeDrawing.Of(source, Geometries.InUse)));
            Assert.That(classes, Has.Some.EqualTo("notebar").And.None.EqualTo("titlebar"));
        });
    }
}
