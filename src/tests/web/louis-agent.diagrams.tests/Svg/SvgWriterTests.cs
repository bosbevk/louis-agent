using System.Xml.Linq;
using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Svg;
using LouisAgent.Diagrams.Engine.Svg.Theme;

namespace LouisAgent.Diagrams.Tests.Svg;

/// <summary>
/// A drawing can define a paint (F134.9): a fade a style fills shapes with by its id, since a style alone can't
/// give a shape a colour that runs into another.
/// </summary>
public class SvgWriterTests
{
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

    /// <summary>What a writer wrote into the file's definitions, the arrowheads left out.</summary>
    private static List<XElement> Paints(SvgWriter writer) =>
    [
        .. XElement.Parse(writer.Write(SvgTheme.Site, "A picture", 100, 100)).Element(Svg + "defs")!.Elements()
            .Where(defined => defined.Name.LocalName != "marker")
    ];

    /// <summary>
    /// A paint is written once under its id, with a stop at each place it was given, each stop classed by the
    /// paint's id and its number, which is how the style colours it.
    /// </summary>
    [Test]
    public void APaint_IsWrittenUnderItsId_WithAStopForEachPlace()
    {
        var paint = Paints(new SvgWriter().Paint("fade", PaintRun.Across, 0, 0.2, 0.8, 1)).Single();

        Assert.Multiple(() =>
        {
            Assert.That((string)paint.Attribute("id")!, Is.EqualTo("fade"));
            Assert.That(paint.Elements(Svg + "stop").Select(stop => (string)stop.Attribute("class")!),
                Is.EqualTo(new[] { "fade-0", "fade-1", "fade-2", "fade-3" }));
            Assert.That(paint.Elements(Svg + "stop").Select(stop => (string)stop.Attribute("offset")!),
                Is.EqualTo(new[] { "0", "0.2", "0.8", "1" }));
        });
    }

    /// <summary>
    /// A paint runs across a shape, down it, from its top left corner to its bottom right, or from its middle out.
    /// </summary>
    [TestCase(nameof(PaintRun.Across), "linearGradient", "1", "0")]
    [TestCase(nameof(PaintRun.Down), "linearGradient", "0", "1")]
    [TestCase(nameof(PaintRun.Slant), "linearGradient", "1", "1")]
    public void APaint_RunsTheWayItWasAsked(string run, string element, string x2, string y2)
    {
        // The run by its name, since the writer's own types are not a public test's to take.
        var paint = Paints(new SvgWriter().Paint("fade", Enum.Parse<PaintRun>(run), 0, 1)).Single();

        Assert.Multiple(() =>
        {
            Assert.That(paint.Name.LocalName, Is.EqualTo(element));
            Assert.That(((string)paint.Attribute("x1")!, (string)paint.Attribute("y1")!), Is.EqualTo(("0", "0")));
            Assert.That(((string)paint.Attribute("x2")!, (string)paint.Attribute("y2")!), Is.EqualTo((x2, y2)));
        });
    }

    /// <summary>A paint from the middle out is a radial one, round the middle of the shape it fills.</summary>
    [Test]
    public void APaintFromTheMiddleOut_IsRadial() =>
        Assert.That(Paints(new SvgWriter().Paint("glow", PaintRun.Out, 0, 1)).Single().Name.LocalName,
            Is.EqualTo("radialGradient"));

    /// <summary>A paint asked for twice is written once, so a file holds no id twice.</summary>
    [Test]
    public void APaintAskedForTwice_IsWrittenOnce() =>
        Assert.That(Paints(new SvgWriter().Paint("fade", PaintRun.Across, 0, 1).Paint("fade", PaintRun.Across, 0, 1)),
            Has.Count.EqualTo(1));

    /// <summary>A part drawn by another writer brings its paints with it when it is set into a picture.</summary>
    [Test]
    public void APartSetIntoAPicture_BringsItsPaints()
    {
        var box = Geometries.InUse.Box(0, 0, 10, 10);
        var part = new SvgWriter().Paint("fade", PaintRun.Down, 0, 1).Box(box, 2, "filled");

        var picture = new SvgWriter().Group("part", 5, 5, part);

        Assert.That(Paints(picture).Select(paint => (string)paint.Attribute("id")!), Is.EqualTo(new[] { "fade" }));
    }

    /// <summary>A drawing that defines no paint has none, as every picture before this had.</summary>
    [Test]
    public void ADrawingWithNoPaint_DefinesNone() => Assert.That(Paints(new SvgWriter()), Is.Empty);
}
