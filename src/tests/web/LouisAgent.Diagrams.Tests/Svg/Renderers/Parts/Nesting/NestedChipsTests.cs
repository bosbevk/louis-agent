using System.Xml.Linq;
using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Nested;
using LouisAgent.Diagrams.Engine.Svg;
using LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.Nesting;
using LouisAgent.Diagrams.Engine.Svg.Theme;

namespace LouisAgent.Diagrams.Tests.Svg.Renderers.Parts.Nesting;

/// <summary>
/// CL65.6: a chip was drawn by a nested picture and by a composite structure, each with the chip's height and
/// where its words stand written out. It is drawn in one place now, so the two can't come to differ (ADR 0123).
/// </summary>
public class NestedChipsTests
{
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

    /// <summary>A shape's attribute, as it is written.</summary>
    private static string? At(XElement shape, string name) => (string?)shape.Attribute(name);

    /// <summary>
    /// A chip is a small box as wide as its words need and as round as it is tall, tinted in its tone, with its
    /// words a little in from its left.
    /// </summary>
    [Test]
    public void AChip_IsARoundBoxAsWideAsItsWords_InItsTone()
    {
        var writer = NestedChips.Draw(new SvgWriter(), Geometries.InUse, left: 100, top: 40, "page", " tone-blue");

        var drawn = XElement.Parse(writer.Write(SvgTheme.Site, "A chip", 400, 200)).Elements().ToList();
        var box = drawn.Single(shape => At(shape, "class") == "pic-tint tone-blue");
        var words = drawn.Single(shape => shape.Name == Svg + "text");

        Assert.Multiple(() =>
        {
            Assert.That(double.Parse(At(box, "width")!, System.Globalization.CultureInfo.InvariantCulture),
                Is.EqualTo(NestedSizes.ChipWidth("page")).Within(0.05));
            Assert.That((At(box, "x"), At(box, "y"), At(box, "height"), At(box, "rx")),
                Is.EqualTo(("100", "40", "18", "9")));
            Assert.That((words.Value, At(words, "x"), At(words, "y"), At(words, "class")),
                Is.EqualTo(("page", "108", "52.5", "cmp-chip-text pic-ink tone-blue")));
        });
    }
}
