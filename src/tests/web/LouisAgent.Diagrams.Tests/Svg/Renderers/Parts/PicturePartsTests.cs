using System.Xml.Linq;
using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Svg;
using LouisAgent.Diagrams.Engine.Svg.Renderers.Parts;
using LouisAgent.Diagrams.Engine.Svg.Theme;

namespace LouisAgent.Diagrams.Tests.Svg.Renderers.Parts;

/// <summary>
/// CL65.6: a class chart's card, a cards picture's glass card and a sequence's frame each drew the accent along
/// their top by the same sum. It is written once, on the box it runs along (ADR 0123).
/// </summary>
public class PicturePartsTests
{
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

    /// <summary>The accent a writer drew along a box, as its one shape.</summary>
    private static XElement Accent(double left, double top, double width, double inset)
    {
        var box = Geometries.InUse.Box(left, top, width, 60);
        string drawn = PictureParts.Accent(new SvgWriter(), Geometries.InUse, box, inset)
            .Write(SvgTheme.Site, "A card", 400, 200);
        return XElement.Parse(drawn).Elements(Svg + "rect")
            .Single(shape => (string?)shape.Attribute("class") == "mrq-edge");
    }

    /// <summary>
    /// The accent is a thin strip on the box's top edge, starting and ending a way in from its sides, and a box
    /// too narrow for that has an accent of no width, never one that runs backwards.
    /// </summary>
    [Test]
    public void TheAccent_RunsAlongABoxsTop_InFromItsSides()
    {
        XElement wide = Accent(100, 40, 200, 28), narrow = Accent(100, 40, 40, 28);

        Assert.Multiple(() =>
        {
            Assert.That(((string?)wide.Attribute("x"), (string?)wide.Attribute("y"), (string?)wide.Attribute("width"),
                (string?)wide.Attribute("height")), Is.EqualTo(("128", "39.5", "144", "1.6")));
            Assert.That((string?)narrow.Attribute("width"), Is.EqualTo("0"));
        });
    }
}
