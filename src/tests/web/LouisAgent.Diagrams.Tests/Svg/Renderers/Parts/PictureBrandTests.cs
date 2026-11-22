using System.Xml.Linq;
using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Svg;
using LouisAgent.Diagrams.Engine.Svg.Renderers.Parts;

namespace LouisAgent.Diagrams.Tests.Svg.Renderers.Parts;

/// <summary>
/// Every picture carries the louis-agent brand, in its footer (F164.1, Louis: "every image must have our branding",
/// then "Distinguise between header and footer on image", his pick "brand in the footer"): the mark and the
/// wordmark at the footer's right, the mark the site's own.
/// </summary>
public class PictureBrandTests
{
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

    /// <summary>The repo's root, found upward from the test's build output.</summary>
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(dir.FullName, "louis-agent-solution.sln"))) dir = dir.Parent!;
        return dir.FullName;
    }

    /// <summary>A picture with a body and nothing else: no key and no caption.</summary>
    private static XElement Bare() => XElement.Parse(PictureBuilder.Titled("The engine", Geometries.InUse)
        .Type("Flow chart")
        .Body("body", new SvgWriter().Box(Geometries.InUse.Box(0, 0, 300, 100), 0, "box"), 300, 100).Build());

    /// <summary>The brand's group in a picture.</summary>
    private static XElement Brand(XElement svg) =>
        svg.Elements(Svg + "g").Single(group => (string?)group.Attribute("class") == "pic-brand");

    /// <summary>
    /// The brand is the site's wordmark (Louis: "must look like this one just a bit smaller"): "l", the mark as its
    /// "o", then "uis-agent", one word at the picture's right edge, heard as louis-agent. The mark is Geist's own bullet,
    /// so it is drawn from the picture's own font (F169.3).
    /// </summary>
    [Test]
    public void TheBrand_IsTheSitesWordmark_AtTheRight()
    {
        var name = Brand(Bare()).Element(Svg + "text")!;
        var mark = name.Element(Svg + "tspan")!;

        Assert.Multiple(() =>
        {
            Assert.That(name.Nodes().Select(node => node is XElement span ? $"[{span.Value}]" : node.ToString()),
                Is.EqualTo(new[] { "l", "[•]", "[uis-agent]" }), "the mark in the place of the o, nothing between");
            Assert.That(mark.Attribute("class")?.Value, Is.EqualTo("pic-mark"));
            Assert.That(name.Attribute("aria-label")?.Value, Is.EqualTo("louis-agent"));
            Assert.That(name.Attribute("text-anchor")?.Value, Is.EqualTo("end"));
            Assert.That(name.Attribute("x")?.Value, Is.EqualTo(SvgWriter.N(300 - PictureBuilder.Inset)),
                "in a little from the picture's right edge");
        });
    }

    /// <summary>
    /// F169.3: the ball is the header's share of the letters and stands on their line (Louis's pick A of three, "Geist's
    /// own bullet, in the line"): the bullet at 1.41 times the letters draws a disc 0.41 of them, set down a tenth of
    /// a letter, and the letters after it lifted back by as much, so they keep the line.
    /// </summary>
    [Test]
    public void TheBall_IsTheHeadersShare_AndStandsOnTheLine()
    {
        var spans = Brand(Bare()).Element(Svg + "text")!.Elements(Svg + "tspan").ToList();

        Assert.Multiple(() =>
        {
            Assert.That(PictureBrand.Style, Does.Contain(".pic-mark { fill: #6b4dff; font-size: 1.41em; }"));
            Assert.That(spans[0].Attribute("dy")?.Value, Is.EqualTo("1.5"), "a tenth of a 15 pixel letter down");
            Assert.That(spans[1].Attribute("dy")?.Value, Is.EqualTo("-1.5"), "and the letters after it back up");
        });
    }

    /// <summary>
    /// The header's words stand in a little from the picture's sides, as the brand does (Louis: "must also move in a
    /// bit"): the title from the left, the kind from the right.
    /// </summary>
    [Test]
    public void TheHeadersWords_StandInFromTheSides()
    {
        var texts = Bare().Elements(Svg + "text").ToList();
        string X(string cls) => texts.Single(text => (string?)text.Attribute("class") == cls).Attribute("x")!.Value;

        Assert.Multiple(() =>
        {
            Assert.That(PictureBuilder.Inset, Is.GreaterThan(0));
            Assert.That(X("mrq-title"), Is.EqualTo(SvgWriter.N(PictureBuilder.Inset)), "the title");
            Assert.That(X("pic-group"), Is.EqualTo(SvgWriter.N(300 - PictureBuilder.Inset)), "the kind");
        });
    }

    /// <summary>
    /// The header's hairline runs under the title, across the picture; a picture with no key still has its footer's
    /// hairline over the brand.
    /// </summary>
    [Test]
    public void TheHeaderAndTheFooter_EachHaveTheirHairline()
    {
        var rules = Bare().Elements(Svg + "rect").Where(rect => (string?)rect.Attribute("class") == "mrq-rule")
            .Select(rect => double.Parse(rect.Attribute("y")!.Value, System.Globalization.CultureInfo.InvariantCulture))
            .ToList();

        Assert.Multiple(() =>
        {
            Assert.That(rules, Has.Count.EqualTo(2));
            Assert.That(rules[0], Is.GreaterThan(PictureBuilder.TitleLine).And.LessThan(PictureMeasures.Usual.Head),
                "under the title, above the body");
            Assert.That(rules[1], Is.GreaterThan(PictureMeasures.Usual.Head + 100), "under the body");
        });
    }

    /// <summary>The mark is the site's own: the violet of the flat theme's drawing of the mark.</summary>
    [Test]
    public void TheMark_IsTheSitesOwnViolet()
    {
        string site = Path.Combine(RepoRoot(), "src/web/LouisAgent.Web.Themes/wwwroot/flat/light/mark-o.svg");
        string violet = XElement.Parse(File.ReadAllText(site)).Elements(Svg + "circle").Single().Attribute("fill")!.Value;

        Assert.That(violet, Is.EqualTo(PictureBrand.Mark));
    }
}
