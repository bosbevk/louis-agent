using System.Xml.Linq;
using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Keys;
using LouisAgent.Diagrams.Engine.Svg;
using LouisAgent.Diagrams.Engine.Svg.Theme;

namespace LouisAgent.Diagrams.Tests.Keys;

/// <summary>
/// CL65.6: three kinds' keys each drew the same rule, heading, row and sample by sums of their own. What a key
/// is drawn with is written once, on boxes, so the keys stand the same because they ask the same code (ADR 0123).
/// </summary>
public class KeyPartsTests
{
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";
    private static readonly IGeometry Geometry = Geometries.InUse;

    /// <summary>What a writer drew, as the shapes of one SVG.</summary>
    private static List<XElement> Drawn(SvgWriter writer) =>
        [.. XElement.Parse(writer.Write(SvgTheme.Site, "A key", 400, 200)).Elements().Where(shape =>
            shape.Name == Svg + "rect" || shape.Name == Svg + "text")];

    /// <summary>A shape's attribute, as it is written.</summary>
    private static string? At(XElement shape, string name) => (string?)shape.Attribute(name);

    /// <summary>
    /// A key starts with a thin rule across the picture at its top, in the kind's own look, and its heading
    /// under the rule at the left.
    /// </summary>
    [Test]
    public void AKeysHead_IsARuleAcrossThePicture_AndItsHeadingUnderIt()
    {
        var drawn = Drawn(KeyParts.Head(new SvgWriter(), Geometry, top: 300, width: 640));
        var rule = drawn.Single(shape => At(shape, "class") == "mrq-rule");
        var heading = drawn.Single(shape => shape.Name == Svg + "text");

        Assert.Multiple(() =>
        {
            Assert.That((At(rule, "x"), At(rule, "y"), At(rule, "width"), At(rule, "height")),
                Is.EqualTo(("0", "300", "640", "1")));
            Assert.That((heading.Value, At(heading, "x"), At(heading, "y"), At(heading, "class")),
                Is.EqualTo(("How to read it", "0", "322", "pic-group")));
        });
    }

    /// <summary>
    /// An item's row is where its layout put it, as wide as its sample and a row tall. A sample stands at the
    /// row's left on its middle line, and a sample line runs along that line from the row's left to its right.
    /// </summary>
    [Test]
    public void AnItemsRow_HoldsItsSample_OnItsMiddleLine()
    {
        var row = KeyParts.Row(Geometry, x: 120, y: 334, sample: 30);
        var badge = KeyParts.Sample(Geometry, row, wide: 18, tall: 18);

        Assert.Multiple(() =>
        {
            Assert.That((row.Left, row.Top, row.Width, row.Height), Is.EqualTo((120.0, 334.0, 30.0, KeyRows.Row)));
            Assert.That((badge.Left, badge.Width, badge.Height), Is.EqualTo((120.0, 18.0, 18.0)));
            Assert.That(badge.Middle.Y, Is.EqualTo(row.Middle.Y));
            Assert.That(KeyParts.Line(row), Is.EqualTo("M120 348H150"));
        });
    }

    /// <summary>An item's words start a little after its sample, a little under the row's middle line.</summary>
    [Test]
    public void AnItemsWords_StandAfterItsSample()
    {
        var row = KeyParts.Row(Geometry, x: 120, y: 334, sample: 30);
        var words = Drawn(KeyParts.Words(new SvgWriter(), row, "a call", "pic-means"))
            .Single(shape => shape.Name == Svg + "text");

        Assert.That((words.Value, At(words, "x"), At(words, "y"), At(words, "class"), At(words, "text-anchor")),
            Is.EqualTo(("a call", "160", "352.5", "pic-means", "start")));
    }
}
