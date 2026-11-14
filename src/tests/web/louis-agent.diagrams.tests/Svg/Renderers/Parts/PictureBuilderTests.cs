using System.Globalization;
using System.Xml.Linq;
using LouisAgent.Diagrams.Engine.Svg;
using LouisAgent.Diagrams.Engine.Svg.Renderers.Parts;

namespace LouisAgent.Diagrams.Tests.Svg.Renderers.Parts;

/// <summary>
/// CL65.2: one builder puts every kind's picture together (Louis: "then use builder pattern for implementation").
/// It owns what six kinds did the same way, each in its own method: the order of the parts, the picture's width
/// and height, the light and where the caption stands. A kind says only what is its own.
/// </summary>
public class PictureBuilderTests
{
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

    /// <summary>A body 300 wide: one box, so it can be found in the picture.</summary>
    private static SvgWriter Box(string cls) => new SvgWriter().Box(0, 0, 300, 100, 0, cls);

    /// <summary>A picture with a body, a key 40 tall and a caption, as a flow would build it.</summary>
    private static PictureBuilder Whole(string title = "The engine") => PictureBuilder.Titled(title)
        .Type("Flow chart").Body("body", Box("box"), 300, 100)
        .Key(width => (40, (writer, top) => writer.Box(0, top, width, 40, 0, "key")))
        .Caption("What it says.").Style(".a { }", ".b { }");

    /// <summary>The picture's shapes after its ground, each by its class, in the order they are drawn.</summary>
    private static List<string> Parts(string svg) =>
    [
        .. XElement.Parse(svg).Elements().Where(shape => shape.Name.LocalName is "path" or "text" or "g" or "rect")
            .Select(shape => (string?)shape.Attribute("class") ?? "").Where(cls => cls != "ground")
    ];

    /// <summary>A number of the picture's root, as the SVG writes it.</summary>
    private static double Root(string svg, string name) =>
        double.Parse((string)XElement.Parse(svg).Attribute(name)!, CultureInfo.InvariantCulture);

    /// <summary>
    /// The parts come in one order, whatever order they were told in: the light, the title, the type, the body,
    /// the key, the caption.
    /// </summary>
    [Test]
    public void TheParts_ComeInOneOrder() => Assert.That(Parts(Whole().Build()),
        Is.EqualTo(new[] { "mrq-light", "mrq-title", "pic-group", "body", "key", "caption" }));

    /// <summary>
    /// The picture is as wide as its body, or as its title and type side by side when that is wider. A narrow
    /// picture is as wide as its body whatever its title. The key is laid out for the width the picture has.
    /// </summary>
    [Test]
    public void TheWidth_IsTheBodys_OrTheTitlesAndTypes()
    {
        string longTitle = new('t', 100);
        double head = PictureBuilder.HeadWidth(longTitle, "Flow chart");
        var key = XElement.Parse(Whole(longTitle).Build()).Elements(Svg + "rect")
            .Single(shape => (string?)shape.Attribute("class") == "key");

        Assert.Multiple(() =>
        {
            Assert.That(Root(Whole().Build(), "width"), Is.EqualTo(300 + 2 * SvgWriter.Pad));
            Assert.That(head, Is.GreaterThan(100 * PictureBuilder.TitleChar + "Flow chart".Length * 7));
            Assert.That(Root(Whole(longTitle).Build(), "width"), Is.EqualTo(head + 2 * SvgWriter.Pad).Within(0.11));
            Assert.That((string?)key.Attribute("width"), Is.EqualTo(SvgWriter.N(head)), "the key has that width");
            Assert.That(Root(Whole(longTitle).Narrow().Build(), "width"), Is.EqualTo(300 + 2 * SvgWriter.Pad));
        });
    }

    /// <summary>
    /// The height adds the room above, the body, the room before the key, the key, and a caption's room: 44, 100,
    /// 26, 40 and 34 by the usual measures, and by a kind's own when it hands them in. With no key and no
    /// caption it is the room above and the body.
    /// </summary>
    [Test]
    public void TheHeight_AddsItsPartsUp()
    {
        var own = new PictureMeasures(Head: 48, KeyGap: 12);
        string bare = PictureBuilder.Titled("The engine").Body("body", Box("box"), 300, 100).Build();
        string sequence = PictureBuilder.Titled("The engine", own).Body("body", Box("box"), 300, 100)
            .Key(_ => (40, (_, _) => { })).Build();
        var body = XElement.Parse(sequence).Elements(Svg + "g").Single();

        Assert.Multiple(() =>
        {
            Assert.That(Root(Whole().Build(), "height"), Is.EqualTo(44 + 100 + 26 + 40 + 34 + 2 * SvgWriter.Pad));
            Assert.That(Root(bare, "height"), Is.EqualTo(44 + 100 + 2 * SvgWriter.Pad));
            Assert.That(Root(sequence, "height"), Is.EqualTo(48 + 100 + 12 + 40 + 2 * SvgWriter.Pad));
            Assert.That((string?)body.Attribute("transform"), Is.EqualTo("translate(0 48)"),
                "the body, under the room");
        });
    }

    /// <summary>The light's fade is defined first, and a kind's own after it, in the order they were called.</summary>
    [Test]
    public void TheFades_AreDefinedInTheOrderCalled_AfterTheLights()
    {
        string svg = PictureBuilder.Titled("The engine").Paint("second", PaintRun.Across, 0, 1)
            .Paint("third", PaintRun.Down, 0, 1).Body("body", Box("box"), 300, 100).Build();
        var ids = XElement.Parse(svg).Element(Svg + "defs")!.Elements()
            .Where(paint => paint.Name.LocalName.EndsWith("Gradient")).Select(paint => (string?)paint.Attribute("id"));

        Assert.That(ids, Is.EqualTo(new[] { "mrq-light", "second", "third" }));
    }

    /// <summary>
    /// A kind's own lights stand where the one behind the top would, before the title. And a body drawn straight
    /// in is not wrapped in a group.
    /// </summary>
    [Test]
    public void AKindsOwnLights_ReplaceTheOneBehindTheTop_AndABodyMayBeDrawnStraightIn()
    {
        string svg = PictureBuilder.Titled("The engine")
            .Lights(writer => writer.Box(0, 0, 10, 10, 0, "own-light").Box(0, 0, 10, 10, 0, "own-light"))
            .Draw(writer => writer.Box(0, 50, 300, 100, 0, "straight"), 300, 100).Build();

        Assert.That(Parts(svg), Is.EqualTo(new[] { "own-light", "own-light", "mrq-title", "straight" }));
    }

    /// <summary>
    /// The picture is named by its title. The words in the title's line may say more, as a component diagram's
    /// do with what stands under its name. The styles are joined in the order given.
    /// </summary>
    [Test]
    public void ThePictureIsNamedByItsTitle_AndItsHeadingMaySayMore()
    {
        var svg = XElement.Parse(Whole().Heading("The engine: where it is").Build());

        Assert.Multiple(() =>
        {
            Assert.That((string?)svg.Attribute("aria-label"), Is.EqualTo("The engine"));
            Assert.That(svg.Elements(Svg + "text").First().Value, Is.EqualTo("The engine: where it is"));
            Assert.That(svg.Elements(Svg + "style").Last().Value, Is.EqualTo(".a { }.b { }"));
        });
    }
}
