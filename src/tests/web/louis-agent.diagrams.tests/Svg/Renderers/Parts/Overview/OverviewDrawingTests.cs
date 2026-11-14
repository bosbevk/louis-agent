using System.Xml.Linq;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Svg.Renderers.Parts.Overview;
using LouisAgent.Diagrams.Svg.Theme.Looks;

namespace LouisAgent.Diagrams.Tests.Svg.Renderers.Parts.Overview;

/// <summary>
/// A cards picture drawn in the new look (F134.9): its own title, a highlighted card as a headline on its light, a
/// heading as a quieter one, plain cards as glass or quiet, and each rail with its lines, nodes, arrowheads and
/// dots. The drawing decides no place itself, so these tests are about what is drawn and how it is marked.
/// </summary>
public class OverviewDrawingTests
{
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

    /// <summary>
    /// A picture with every look and every kind of link: a heading over two cards, the two feeding a highlighted
    /// engine, the engine linked both ways to one card and up from another, and a card no link touches.
    /// </summary>
    private const string Layers = """
                                  {
                                    "kind": "cards",
                                    "title": "The layers at a glance",
                                    "cards": {
                                      "rows": [
                                        { "cards": [
                                          { "id": "app", "icon": "code", "title": "Your application",
                                            "text": "The business problem.", "look": "heading" }
                                        ] },
                                        { "cards": [
                                          { "id": "left", "icon": "user", "title": "Identity", "code": "identities/",
                                            "text": "Who the agent is, its roles, voice and rules." },
                                          { "id": "right", "icon": "box",
                                            "title": "Everything an agent can do, as packages your own program names",
                                            "text": "What the agent can do through tools." }
                                        ] },
                                        { "cards": [
                                          { "id": "engine", "icon": "chip", "title": "Core engine",
                                            "code": "louis-agent.core", "text": "The loop and its guards.",
                                            "look": "highlight" }
                                        ] },
                                        { "cards": [
                                          { "id": "one", "icon": "cloud", "title": "Model providers",
                                            "text": "Claude, Ollama and others." },
                                          { "id": "two", "icon": "checks", "title": "Evaluation",
                                            "text": "Recorded runs and traces." }
                                        ] },
                                        { "cards": [
                                          { "id": "loose", "icon": "file", "title": "What else it has",
                                            "text": "A part no link touches." }
                                        ] }
                                      ],
                                      "links": [
                                        { "from": "app", "to": "left" }, { "from": "app", "to": "right" },
                                        { "from": "left", "to": "engine" }, { "from": "right", "to": "engine" },
                                        { "from": "engine", "to": "one", "kind": "both" },
                                        { "from": "engine", "to": "two", "kind": "up" }
                                      ]
                                    }
                                  }
                                  """;

    private static string Drawn(string json = Layers) => OverviewDrawing.Of(DiagramReader.Read(json));

    /// <summary>Every shape of a drawing that has a class, with that class.</summary>
    private static List<(XElement Shape, string Class)> Shapes(string svg) =>
    [
        .. XElement.Parse(svg).Descendants().Where(shape => shape.Attribute("class") is not null)
            .Select(shape => (shape, (string)shape.Attribute("class")!))
    ];

    /// <summary>The shapes drawn in a class.</summary>
    private static List<XElement> In(string cls, string? svg = null) =>
        [.. Shapes(svg ?? Drawn()).Where(shape => shape.Class.Split(' ').Contains(cls)).Select(shape => shape.Shape)];

    /// <summary>The whole picture, byte for byte, so a change to the look is a change someone reads.</summary>
    [Test]
    public void TheLayers_AreDrawnAsTheirGolden() => Golden.Match("overview-look.svg", Drawn());

    /// <summary>The picture carries its own title, small, and the shared look with the cards' own after it.</summary>
    [Test]
    public void ThePicture_HasItsOwnTitle_AndItsLook()
    {
        string svg = Drawn();

        Assert.Multiple(() =>
        {
            Assert.That(In("crd-title", svg).Single().Value, Is.EqualTo("The layers at a glance"));
            Assert.That(svg, Does.Contain(".pic-march").And.Contain(".crd-rail"));
            Assert.That(svg.IndexOf(".pic-march", StringComparison.Ordinal),
                Is.LessThan(svg.IndexOf(".crd-rail", StringComparison.Ordinal)), "the shared look first");
        });
    }

    /// <summary>
    /// A highlighted card is a headline with no box: a badge, its title large, its code name in a pill, on one
    /// light. A heading is a quieter headline, with neither badge nor light.
    /// </summary>
    [Test]
    public void AHighlightedCard_IsAHeadlineOnItsLight_AndAHeadingAQuieterOne() => Assert.Multiple(() =>
    {
        Assert.That(In("crd-headline").Single().Value, Is.EqualTo("Core engine"));
        Assert.That(In("crd-heading").Single().Value, Is.EqualTo("Your application"));
        Assert.That(In("crd-badge"), Has.Count.EqualTo(1));
        Assert.That(In("crd-light"), Has.Count.EqualTo(1));
        Assert.That(In("crd-code").Select(code => code.Value), Does.Contain("louis-agent.core"));
    });

    /// <summary>
    /// A plain card a link touches is glass, with its accent along the top and its icon in a tinted badge. One no
    /// link touches is quiet, with neither.
    /// </summary>
    [Test]
    public void APlainCard_IsGlass_AndOneNoLinkTouchesIsQuiet() => Assert.Multiple(() =>
    {
        Assert.That(In("crd-glass"), Has.Count.EqualTo(4), "left, right, one and two");
        Assert.That(In("crd-edge"), Has.Count.EqualTo(4));
        Assert.That(In("crd-tint"), Has.Count.EqualTo(4));
        Assert.That(In("crd-quiet"), Has.Count.EqualTo(1));
        Assert.That(In("crd-name").Select(name => (string)name.Attribute("class")!),
            Has.Exactly(1).Contains("quiet"));
    });

    /// <summary>A long title is drawn on the lines the layout broke it into, every word of it.</summary>
    [Test]
    public void ALongTitle_IsDrawnOnItsLines()
    {
        var names = In("crd-name").Select(name => name.Value).ToList();

        const string title = "Everything an agent can do, as packages your own program names";
        Assert.That(string.Join(" ", names), Does.Contain(title));
        Assert.That(names, Has.None.EqualTo(title), "over two lines");
    }

    /// <summary>
    /// Each rail has its line and its glow, and each of its ends a line and a node. Three rails here, with three
    /// ends each.
    /// </summary>
    [Test]
    public void ARail_HasItsLineItsGlow_AndALineAndANodeForEachEnd() => Assert.Multiple(() =>
    {
        Assert.That(In("crd-rail"), Has.Count.EqualTo(3));
        Assert.That(In("crd-rail-glow"), Has.Count.EqualTo(3));
        Assert.That(In("crd-line"), Has.Count.EqualTo(9));
        Assert.That(In("crd-node"), Has.Count.EqualTo(9));
    });

    /// <summary>
    /// An arrowhead stands on the end a link points at: at the card below for a link down, and at the card above,
    /// at the line's start, for one up or both ways.
    /// </summary>
    [Test]
    public void AnArrowhead_StandsOnTheEndALinkPointsAt()
    {
        var lines = In("crd-line");

        Assert.Multiple(() =>
        {
            // Down: app to left and right, and the one drop into the engine. Both ways: the drop into "one".
            Assert.That(lines.Count(line => line.Attribute("marker-end") is not null), Is.EqualTo(4));
            // Up and both ways: the one stem under the engine.
            Assert.That(lines.Count(line => line.Attribute("marker-start") is not null), Is.EqualTo(1));
        });
    }

    /// <summary>
    /// Dots run along a rail's lines only where every link of the rail points down, since dots say one way: on the
    /// first two rails' six lines, and not on the third's.
    /// </summary>
    [Test]
    public void Dots_RunOnlyWhereEveryLinkOfARailPointsDown() =>
        Assert.That(In("pic-march"), Has.Count.EqualTo(6));

    /// <summary>The light stands behind everything, so a rail and a card are drawn over it.</summary>
    [Test]
    public void TheLight_StandsBehindTheRest()
    {
        var classes = Shapes(Drawn()).Select(shape => shape.Class).ToList();

        Assert.That(classes.IndexOf("crd-light"), Is.LessThan(classes.IndexOf("crd-rail-glow")));
    }

    /// <summary>A picture with no highlighted card has no light, and one with no links no rail.</summary>
    [Test]
    public void WithNoHighlightedCard_ThereIsNoLight_AndWithNoLinksNoRail()
    {
        // The same picture with its highlighted card made plain and its links taken out.
        string plain = Layers.Replace("\"look\": \"highlight\"", "\"look\": \"plain\"");
        string bare = Drawn(plain[..plain.IndexOf("\"links\"", StringComparison.Ordinal)] + "\"links\": [] } }");

        Assert.Multiple(() =>
        {
            Assert.That(In("crd-light", bare), Is.Empty);
            Assert.That(In("crd-rail", bare), Is.Empty);
            Assert.That(In("crd-quiet", bare), Is.Empty, "with no links, no card is quiet");
        });
    }

    /// <summary>The file defines the look's five paints, each under the id the style fills with.</summary>
    [Test]
    public void TheFile_DefinesTheLooksPaints()
    {
        var ids = XElement.Parse(Drawn()).Element(Svg + "defs")!.Elements()
            .Where(defined => defined.Name.LocalName != "marker").Select(paint => (string)paint.Attribute("id")!);

        Assert.That(ids, Is.EquivalentTo(new[]
        {
            CardsStyle.LightPaint, CardsStyle.RailPaint, CardsStyle.BadgePaint, CardsStyle.EdgePaint,
            CardsStyle.GlassPaint
        }));
    }

    /// <summary>Drawn bare, with its caption, the caption stands under the cards.</summary>
    [Test]
    public void ACaption_StandsUnderTheCards()
    {
        string svg = Drawn(Layers.Replace("\"cards\": {", "\"caption\": \"One engine, many parts.\",\n\"cards\": {"));

        Assert.That(In("caption", svg).Single().Value, Is.EqualTo("One engine, many parts."));
    }
}
