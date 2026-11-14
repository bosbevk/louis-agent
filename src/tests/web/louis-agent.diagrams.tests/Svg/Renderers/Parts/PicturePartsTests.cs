using System.Xml.Linq;
using LouisAgent.Diagrams.Engine.Svg.Renderers.Parts;
using LouisAgent.Diagrams.Engine.Svg.Theme;

namespace LouisAgent.Diagrams.Tests.Svg.Renderers.Parts;

/// <summary>
/// The start every picture in the new look shares (F134.12): the light and the small title. Since F134.13 it can
/// say what type of diagram the picture is, at the right of the title's line (Louis: "the type of diagram must
/// always display").
/// </summary>
public class PicturePartsTests
{
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

    /// <summary>
    /// A picture 600 wide, opened with a title and a type or none, and the texts it has by their class.
    /// </summary>
    private static List<XElement> Texts(string? type, string cls) =>
    [
        .. XElement.Parse(PictureParts.Open("The engine", 600, type).Write(SvgTheme.Site, "The engine", 600, 200))
            .Descendants(Svg + "text").Where(text => (string?)text.Attribute("class") == cls)
    ];

    /// <summary>
    /// A picture opened with a type shows it once, in the small quiet capitals a group's name has, ending at the
    /// picture's right side on the title's line.
    /// </summary>
    [Test]
    public void OpenedWithAType_ItShowsItAtTheRightOfTheTitlesLine()
    {
        var type = Texts("Composite structure diagram", "pic-group").Single();
        var title = Texts("Composite structure diagram", "mrq-title").Single();

        Assert.Multiple(() =>
        {
            Assert.That(type.Value, Is.EqualTo("Composite structure diagram"));
            Assert.That((string?)type.Attribute("x"), Is.EqualTo("600"));
            Assert.That((string?)type.Attribute("text-anchor"), Is.EqualTo("end"));
            Assert.That((string?)type.Attribute("y"), Is.EqualTo((string?)title.Attribute("y")), "on the title's line");
        });
    }

    /// <summary>A picture opened with no type shows none: a kind that has not been given one is as it was.</summary>
    [Test]
    public void OpenedWithNoType_ItShowsNone() => Assert.That(Texts(null, "pic-group"), Is.Empty);

    /// <summary>
    /// A picture is at least as wide as its title and its type side by side, so the two never run into one
    /// another.
    /// </summary>
    [Test]
    public void ThePicturesHead_IsAsWideAsItsTitleAndItsType()
    {
        double alone = PictureParts.HeadWidth("The engine", null);
        double both = PictureParts.HeadWidth("The engine", "Composite structure diagram");

        Assert.Multiple(() =>
        {
            Assert.That(alone, Is.EqualTo("The engine".Length * PictureParts.TitleChar));
            Assert.That(both, Is.GreaterThan(alone + "Composite structure diagram".Length * 7));
        });
    }
}
