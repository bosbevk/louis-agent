using System.Xml.Linq;
using LouisAgent.Diagrams.Engine.Svg;
using LouisAgent.Diagrams.Engine.Svg.Ends;
using LouisAgent.Diagrams.Engine.Svg.Theme;

namespace LouisAgent.Diagrams.Tests.Svg;

/// <summary>
/// A line can end in more than an arrowhead (F134.11): a hollow triangle, a diamond or an open arrowhead, at
/// either end, in a tone. Each is written once, and only when a line wears it.
/// </summary>
public class SvgWriterEndsTests
{
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

    private static XElement Written(SvgWriter writer) =>
        XElement.Parse(writer.Write(SvgTheme.Site, "A picture", 100, 100));

    /// <summary>The ids of the ends a file defines, in the order written.</summary>
    private static List<string> Ends(XElement file) =>
        [.. file.Element(Svg + "defs")!.Elements(Svg + "marker").Select(end => (string)end.Attribute("id")!)];

    private static XElement Line(XElement file, string cls) =>
        file.Descendants(Svg + "path").Single(path => (string?)path.Attribute("class") == cls);

    /// <summary>
    /// A line that ends in a triangle points at its end, and two such lines share the one end. The plain
    /// arrowhead every file has stands before it.
    /// </summary>
    [Test]
    public void ALinesEnd_IsWrittenOnce_AndTheLinePointsAtIt()
    {
        var file = Written(new SvgWriter().Line("M 0 0 L 50 0", "one", end: LineEnd.Triangle)
            .Line("M 0 9 L 50 9", "two", end: LineEnd.Triangle));

        Assert.Multiple(() =>
        {
            Assert.That(Ends(file), Is.EqualTo(new[] { "arrow", "triangle" }));
            Assert.That((string?)Line(file, "one").Attribute("marker-end"), Is.EqualTo("url(#triangle)"));
            Assert.That((string?)Line(file, "two").Attribute("marker-end"), Is.EqualTo("url(#triangle)"));
            Assert.That(Line(file, "one").Attribute("marker-start"), Is.Null);
        });
    }

    /// <summary>
    /// A line may start in one end and finish in another: a diamond where it leaves, an open arrowhead.
    /// </summary>
    [Test]
    public void ALine_MayStartInOneEnd_AndFinishInAnother()
    {
        var file = Written(new SvgWriter().Line("M 0 0 L 50 0", "held", LineEnd.Diamond, LineEnd.Open));

        Assert.Multiple(() =>
        {
            Assert.That((string?)Line(file, "held").Attribute("marker-start"), Is.EqualTo("url(#diamond)"));
            Assert.That((string?)Line(file, "held").Attribute("marker-end"), Is.EqualTo("url(#open)"));
            Assert.That(Ends(file), Is.EqualTo(new[] { "arrow", "diamond", "open" }));
        });
    }

    /// <summary>
    /// An end in a tone is an end of its own, classed by its kind and its tone, which is how a look colours it.
    /// </summary>
    [Test]
    public void AnEndInATone_IsItsOwn_ClassedByKindAndTone()
    {
        var file = Written(new SvgWriter().Line("M 0 0 L 50 0", "base", end: LineEnd.Triangle, tone: "violet")
            .Line("M 0 9 L 50 9", "plain", end: LineEnd.Triangle));
        var toned = file.Element(Svg + "defs")!.Elements(Svg + "marker")
            .Single(end => (string)end.Attribute("id")! == "triangle-violet");

        Assert.Multiple(() =>
        {
            Assert.That(Ends(file), Is.EqualTo(new[] { "arrow", "triangle-violet", "triangle" }));
            Assert.That((string?)Line(file, "base").Attribute("marker-end"), Is.EqualTo("url(#triangle-violet)"));
            Assert.That((string?)toned.Element(Svg + "path")!.Attribute("class"),
                Is.EqualTo("end-triangle tone-violet"));
            Assert.That((string?)toned.Attribute("orient"), Is.EqualTo("auto-start-reverse"), "it turns with its line");
        });
    }

    /// <summary>An arrowhead asked for as an end is the writer's own arrowhead, plain or in its tone.</summary>
    [Test]
    public void AnArrowAsAnEnd_IsTheWritersOwnArrowhead()
    {
        var file = Written(new SvgWriter().Line("M 0 0 L 50 0", "plain", end: LineEnd.Arrow)
            .Line("M 0 9 L 50 9", "toned", LineEnd.Diamond, LineEnd.Arrow, "violet"));

        Assert.Multiple(() =>
        {
            Assert.That((string?)Line(file, "plain").Attribute("marker-end"), Is.EqualTo("url(#arrow)"));
            Assert.That((string?)Line(file, "toned").Attribute("marker-end"), Is.EqualTo("url(#arrow-violet)"));
            Assert.That(Ends(file), Is.EqualTo(new[] { "arrow", "arrow-violet", "diamond-violet" }));
        });
    }

    /// <summary>
    /// A line with no end has none, and a file whose lines wear no end defines only the plain arrowhead.
    /// </summary>
    [Test]
    public void AnEndNoLineWears_IsNotWritten()
    {
        var file = Written(new SvgWriter().Line("M 0 0 L 50 0", "bare"));

        Assert.Multiple(() =>
        {
            Assert.That(Ends(file), Is.EqualTo(new[] { "arrow" }));
            Assert.That(Line(file, "bare").Attributes().Select(attribute => attribute.Name.LocalName),
                Is.EqualTo(new[] { "class", "d" }));
        });
    }

    /// <summary>A part drawn on its own and set down in a picture brings the ends its lines wear.</summary>
    [Test]
    public void APartDrawnElsewhere_BringsItsEnds()
    {
        var part = new SvgWriter().Line("M 0 0 L 50 0", "held", LineEnd.Diamond, tone: "violet");

        Assert.That(Ends(Written(new SvgWriter().Group("part", 0, 10, part))),
            Is.EqualTo(new[] { "arrow", "diamond-violet" }));
    }

    /// <summary>A page's copy of a picture keeps the ends its lines wear.</summary>
    [Test]
    public void APagesCopy_KeepsTheEnds()
    {
        string file = new SvgWriter().Line("M 0 0 L 50 0", "base", end: LineEnd.Triangle, tone: "violet")
            .Write(SvgTheme.Site, "A picture", 100, 100);

        Assert.That(Ends(XElement.Parse(PageSvg.Of(file))), Does.Contain("triangle-violet"));
    }
}
