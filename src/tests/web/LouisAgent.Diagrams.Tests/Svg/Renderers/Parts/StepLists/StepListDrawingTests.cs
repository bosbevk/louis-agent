using System.Globalization;
using System.Xml.Linq;
using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Svg;
using LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.StepLists;
using LouisAgent.Diagrams.Sources;

namespace LouisAgent.Diagrams.Tests.Svg.Renderers.Parts.StepLists;

/// <summary>
/// A sequence drawn as steps down a phone's width (F169.7): one SVG with no lifeline, its frames back to front,
/// the sequence's own key, the type under the title, 338 wide, and no words under 10 pixels.
/// </summary>
public class StepListDrawingTests
{
    /// <summary>The sequence on How it works, as the framework's site writes it, three frames deep.</summary>
    private const string OneTurn = """
        {
          "kind": "sequence",
          "title": "One turn, through every layer",
          "sequence": {
            "participants": [
              { "id": "you", "label": "You", "actor": true, "tone": "blue" },
              { "id": "api", "label": "LouisAgent.Api", "icon": "server", "tone": "violet" },
              { "id": "engine", "label": "AgentEngine", "icon": "chip", "tone": "violet" },
              { "id": "pack", "label": "A pack's tool", "icon": "box", "tone": "green" },
              { "id": "model", "label": "The model", "icon": "cloud", "tone": "red" }
            ],
            "steps": [
              { "step": "message", "from": "you", "to": "api", "text": "POST /v1/sessions/{id}/messages" },
              { "step": "message", "from": "api", "to": "engine", "text": "StreamPromptAsync, with the history" },
              { "step": "block", "block": "loop", "label": "up to 10 tool rounds", "steps": [
                { "step": "message", "from": "engine", "to": "model", "text": "the request, with the tools" },
                { "step": "reply", "from": "model", "to": "engine", "text": "text, or a tool call" },
                { "step": "block", "block": "opt", "label": "a tool call", "steps": [
                  { "step": "message", "from": "engine", "to": "pack", "text": "run it, through the guards" },
                  { "step": "reply", "from": "pack", "to": "engine", "text": "its result" },
                  { "step": "block", "block": "opt", "label": "over 50,000 characters", "steps": [
                    { "step": "message", "from": "engine", "to": "model", "text": "summarise it" }
                  ] }
                ] }
              ] },
              { "step": "note", "over": ["engine", "model"], "text": "every request priced in the ledger" },
              { "step": "reply", "from": "engine", "to": "api", "text": "the answer, as it is written" },
              { "step": "reply", "from": "api", "to": "you", "text": "server-sent events" }
            ]
          }
        }
        """;

    /// <summary>A small sequence with an alt and its other case, numbered.</summary>
    private const string WithAnAlt = """
        {
          "kind": "sequence",
          "title": "A tool call",
          "sequence": {
            "participants": [ { "id": "e", "label": "The engine" }, { "id": "t", "label": "A tool" } ],
            "steps": [
              { "step": "block", "block": "alt", "label": "it works", "steps": [
                { "step": "message", "from": "e", "to": "t", "text": "call" }
              ], "else": [
                { "label": "it fails", "steps": [ { "step": "reply", "from": "t", "to": "e", "text": "why" } ] }
              ] }
            ],
            "numbered": true
          }
        }
        """;

    private static XElement Drawn(string json = OneTurn) =>
        XElement.Parse(StepListDrawing.Of(DiagramReader.Read(json), Geometries.InUse));

    /// <summary>The shapes in a class, anywhere under a shape.</summary>
    private static List<XElement> In(XElement under, string cls) =>
    [
        .. under.Descendants().Where(shape =>
            ((string?)shape.Attribute("class") ?? "").Split(' ').Contains(cls))
    ];

    /// <summary>The steps, apart from the key, which shows a sample of each.</summary>
    private static XElement Steps(XElement svg) => In(svg, "stl-drawing").Single();

    private static double Number(XElement shape, string name) =>
        double.Parse((string)shape.Attribute(name)!, CultureInfo.InvariantCulture);

    /// <summary>The picture is one SVG with every step, and no lifeline and no head.</summary>
    [Test]
    public void ThePicture_IsOneSvg_WithNoLifeline()
    {
        var svg = Drawn();

        Assert.Multiple(() =>
        {
            Assert.That(svg.Name.LocalName, Is.EqualTo("svg"));
            Assert.That(In(svg, "seq-life"), Is.Empty);
            Assert.That(In(Steps(svg), "seq-badge"), Is.Empty, "the key's samples are the only badges");
            Assert.That(In(Steps(svg), "stl-bar"), Has.Count.EqualTo(9), "nine calls and answers");
            Assert.That(In(Steps(svg), "mrq-glass"), Has.Count.EqualTo(4), "three frames and a note");
        });
    }

    /// <summary>The three frames are drawn, the outer before the inner, each with its kind in a pill.</summary>
    [Test]
    public void TheFrames_AreDrawn_OuterBeforeInner()
    {
        var steps = Steps(Drawn());
        var kinds = In(steps, "seq-kind").Select(pill => Number(pill, "x")).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(In(steps, "mrq-code").Select(word => word.Value), Is.EqualTo(new[] { "loop", "opt", "opt" }));
            Assert.That(kinds, Is.Ordered.Ascending);
        });
    }

    /// <summary>An alt's other case is drawn under its divider, with its "else" pill.</summary>
    [Test]
    public void AnAltsOtherCase_IsDrawnUnderItsDivider()
    {
        var steps = Steps(Drawn(WithAnAlt));

        Assert.Multiple(() =>
        {
            Assert.That(In(steps, "seq-divide"), Has.Count.EqualTo(1));
            Assert.That(In(steps, "seq-else-word").Single().Value, Is.EqualTo("else"));
            Assert.That(In(steps, "seq-count").Select(count => count.Value), Is.EqualTo(new[] { "1", "2" }));
        });
    }

    /// <summary>The sequence's own key is under the steps, with a call's, an answer's and a note's sample.</summary>
    [Test]
    public void TheKey_IsTheSequencesOwn()
    {
        var key = In(Drawn(), "seq-legend").Single();

        Assert.Multiple(() =>
        {
            Assert.That(In(key, "seq-beam"), Is.Not.Empty);
            Assert.That(In(key, "seq-answer"), Is.Not.Empty);
            Assert.That(In(key, "seq-swatch"), Is.Not.Empty);
        });
    }

    /// <summary>The type stands under the title, as on every picture for a phone; the picture is 338 wide.</summary>
    [Test]
    public void TheType_IsUnderTheTitle_AndThePictureIs338Wide()
    {
        var svg = Drawn();
        var title = In(svg, "mrq-title").Single();
        var type = In(svg, "pic-group").First();

        Assert.Multiple(() =>
        {
            Assert.That(Number(type, "y"), Is.GreaterThan(Number(title, "y")));
            Assert.That((string)type.Attribute("text-anchor")!, Is.EqualTo("start"));
            Assert.That(Number(svg, "width"), Is.EqualTo(PageSvg.NarrowLayout + 2 * SvgWriter.Pad));
        });
    }

    /// <summary>No style in the picture sets words smaller than 10 pixels.</summary>
    [Test]
    public void NoStyle_SetsWordsUnderTenPixels()
    {
        string style = string.Concat(Drawn().Elements().Where(part => part.Name.LocalName == "style")
            .Select(part => part.Value));
        var sizes = style.Split("font:").Skip(1)
            .Select(rule => rule.Split("px")[0].Split(' ')[^1])
            .Select(size => double.Parse(size, CultureInfo.InvariantCulture))
            .ToList();

        Assert.Multiple(() =>
        {
            Assert.That(sizes, Is.Not.Empty);
            Assert.That(sizes, Has.All.GreaterThanOrEqualTo(10));
        });
    }
}
