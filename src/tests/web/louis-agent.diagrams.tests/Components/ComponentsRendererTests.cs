using System.Xml.Linq;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Svg;

namespace LouisAgent.Diagrams.Tests.Components;

/// <summary>
/// F134.7: a component diagram is drawn in a look of its own, inside the frame every picture in the docs has. The
/// renderer draws what the layout placed and decides no place itself, so these tests are about what is drawn and
/// how it is marked: every word, each box by how deep it is, each pin by which way it points, and motion that
/// stops for a reader who asked for less.
/// </summary>
public class ComponentsRendererTests
{
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

    /// <summary>The shared sample as the docs draw it, with their frame round it.</summary>
    private static string Drawn() => DiagramRenderers.RenderTitled(DiagramReader.Read(ComponentSamples.Start));

    /// <summary>The classes of every shape that has one, each shape's as one text.</summary>
    private static List<string> Classes(string svg) =>
        [.. XElement.Parse(svg).Descendants().Select(shape => (string?)shape.Attribute("class")).OfType<string>()];

    /// <summary>The whole picture, byte for byte, so a change to the look is a change someone reads.</summary>
    [Test]
    public void TheStart_IsDrawnAsItsGolden() => Golden.Match("components-start.svg", Drawn());

    /// <summary>
    /// Every word of the JSON is in the picture: names, chips, notes, pins, the pill and the lines' words.
    /// </summary>
    [TestCase("ShowroomStart")]
    [TestCase("general part")]
    [TestCase("one forward, the rest behind it")]
    [TestCase("ForwardChanged")]
    [TestCase("which demo is forward")]
    [TestCase("PlainBody")]
    [TestCase("DemosPage")]
    [TestCase("Featured")]
    [TestCase("the site's list")]
    [TestCase("Deck.razor.js")]
    [TestCase("Viewed")]
    [TestCase("How to read it")]
    [TestCase("The start: what it is made of")]
    [TestCase("louis-agent.web, Site / Showroom / Start")]
    [TestCase("Components/Site/Showroom/Start")]
    [TestCase("What the start reads")]
    [TestCase("watch, bring")]
    [TestCase("a parameter")]
    [TestCase("a callback")]
    [TestCase("reads a service")]
    [TestCase("the one thing it holds")]
    [TestCase("A box inside a box is held by it.")]
    public void EveryWord_IsInThePicture(string word)
    {
        var words = XElement.Parse(Drawn()).Descendants(Svg + "text").Select(text => text.Value);

        Assert.That(words, Has.Some.Contains(word));
    }

    /// <summary>
    /// A box is marked by how deep it is nested, so the look can lighten it, and wears its tone. A ghost is marked
    /// as one. A pin said back up is marked apart from one handed down.
    /// </summary>
    [Test]
    public void EachShape_IsMarkedForWhatItIs()
    {
        var classes = Classes(Drawn());

        Assert.Multiple(() =>
        {
            Assert.That(classes.Count(cls => cls.Contains("pic-panel") && cls.Contains("depth-0")), Is.EqualTo(1));
            Assert.That(classes.Count(cls => cls.Contains("pic-panel") && cls.Contains("depth-3")), Is.EqualTo(1),
                "the window, three boxes deep");
            Assert.That(classes.Count(cls => cls.Contains("cmp-ghost")), Is.EqualTo(1));
            Assert.That(classes.Where(cls => cls.Contains("pic-panel")), Has.Some.Contains("tone-green"));
            Assert.That(classes.Count(cls => cls.StartsWith("cmp-pin ") && cls.Contains(" up")), Is.EqualTo(2));
            Assert.That(classes.Count(cls => cls.StartsWith("cmp-pin ")), Is.EqualTo(9));
            Assert.That(classes.Count(cls => cls.StartsWith("cmp-pill ")), Is.EqualTo(1));
            Assert.That(classes.Count(cls => cls.StartsWith("pic-port ")), Is.EqualTo(7),
                "a port where each of the five lines leaves or arrives at the root or the deck, and on what is given");
            Assert.That(classes, Has.None.EqualTo("titlebar"), "the picture has its own title, and no bar");
            Assert.That(classes, Has.Some.EqualTo("notebar"), "the docs' note stays");
            Assert.That(classes.Count(cls => cls.StartsWith("pic-line ") && cls.Contains("reads")), Is.EqualTo(1));
        });
    }

    /// <summary>
    /// The dots that march along a line stop for a reader who asked for less motion, and the picture carries both
    /// its light and its dark colours.
    /// </summary>
    [Test]
    public void MotionStopsOnRequest_AndBothThemesAreThere()
    {
        string drawn = Drawn();

        Assert.Multiple(() =>
        {
            Assert.That(drawn, Does.Contain("@media (prefers-reduced-motion: reduce)"));
            Assert.That(drawn, Does.Contain(".pic-march { display: none; }"));
            Assert.That(drawn, Does.Contain("@media (prefers-color-scheme: dark)"));
            Assert.That(drawn, Does.Contain("--pic-0:"));
        });
    }

    /// <summary>
    /// The docs' frame is the shared one, with its title bar and its note. What each tone means is said once, in
    /// the picture's own key, and not a second time in the frame's legend.
    /// </summary>
    [TestCase("this project")]
    [TestCase("starts it")]
    [TestCase("contracts and state")]
    [TestCase("One thing held, handed down and said back up.")]
    public void TheDocsFrame_HasItsNote_AndATonesMeaningIsSaidOnce(string words)
    {
        var texts = XElement.Parse(Drawn()).Descendants(Svg + "text").Select(text => text.Value);

        Assert.That(texts.Count(text => text.Contains(words)), Is.EqualTo(1));
    }

    /// <summary>On a page the same picture is drawn without the docs' frame.</summary>
    [Test]
    public void OnAPage_ItIsDrawnWithoutTheFrame()
    {
        string drawn = DiagramRenderers.RenderForPage(DiagramReader.Read(ComponentSamples.Start));

        Assert.That(drawn, Does.Contain("ShowroomStart").And.Not.Contain("titlebar"));
    }
}
