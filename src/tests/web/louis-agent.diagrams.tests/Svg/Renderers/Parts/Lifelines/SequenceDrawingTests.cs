using System.Xml.Linq;
using LouisAgent.Diagrams.Engine.Geometry;
using LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.Lifelines;
using LouisAgent.Diagrams.Sources;

namespace LouisAgent.Diagrams.Tests.Svg.Renderers.Parts.Lifelines;

/// <summary>
/// A sequence drawn in the cards' look (F134.10): its own title, each participant a badge in its tone with its
/// name and no box, a lifeline under it, a call as a beam with its node and its dots, an answer as a quiet dashed
/// line, a note and a block as glass, and the key under it all. The drawing decides no place itself, so these
/// tests are about what is drawn and how it is marked.
/// </summary>
public class SequenceDrawingTests
{
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

    /// <summary>
    /// One chat turn, with every kind of step: calls to the right and one to the left, answers, two calls a
    /// participant makes to itself, a note over two lifelines, and blocks three deep with an else. A person with
    /// no icon, a name over two lines, a message over two lines, and a participant with neither tone nor icon.
    /// </summary>
    private const string Turn = """
                                {
                                  "kind": "sequence",
                                  "title": "One chat turn",
                                  "sequence": {
                                    "participants": [
                                      { "id": "you", "label": "You", "actor": true, "tone": "blue" },
                                      { "id": "api", "label": "louis-agent.api", "icon": "server", "tone": "green" },
                                      { "id": "engine", "label": "AgentEngine", "icon": "chip", "tone": "violet" },
                                      { "id": "model", "label": "The model that answers, wherever it runs",
                                        "icon": "cloud", "tone": "red" },
                                      { "id": "log", "label": "A plain one" }
                                    ],
                                    "steps": [
                                      { "step": "message", "from": "you", "to": "api",
                                        "text": "POST /v1/sessions/{id}/messages" },
                                      { "step": "message", "from": "api", "to": "engine",
                                        "text": "RunTurnAsync, with the whole history of the chat and its tools" },
                                      { "step": "block", "block": "loop", "label": "up to 10 tool rounds", "steps": [
                                        { "step": "message", "from": "engine", "to": "model",
                                          "text": "the request, with the tools" },
                                        { "step": "reply", "from": "model", "to": "engine",
                                          "text": "text, or a tool call" },
                                        { "step": "block", "block": "alt", "label": "a tool call", "steps": [
                                          { "step": "message", "from": "engine", "to": "engine",
                                            "text": "run the tool" },
                                          { "step": "block", "block": "opt", "label": "its result is too large",
                                            "steps": [
                                              { "step": "message", "from": "engine", "to": "engine",
                                                "text": "summarise it" }
                                            ] }
                                        ], "else": [
                                          { "label": "the answer", "steps": [
                                            { "step": "reply", "from": "engine", "to": "api", "text": "the text" }
                                          ] }
                                        ] }
                                      ] },
                                      { "step": "note", "over": ["api", "engine"],
                                        "text": "every request is measured in the ledger" },
                                      { "step": "message", "from": "engine", "to": "api", "text": "Record(usage)" },
                                      { "step": "message", "from": "api", "to": "log", "text": "one line" },
                                      { "step": "reply", "from": "api", "to": "you",
                                        "text": "SSE: text, tool calls, cost" }
                                    ],
                                    "numbered": false
                                  }
                                }
                                """;

    private static string Drawn(string json = Turn) => SequenceDrawing.Of(DiagramReader.Read(json), Geometries.InUse);

    /// <summary>The shapes drawn in a class, anywhere in a drawing or under one of its shapes.</summary>
    private static List<XElement> In(string cls, XElement? under = null) =>
    [
        .. (under ?? XElement.Parse(Drawn())).Descendants().Where(shape =>
            ((string?)shape.Attribute("class") ?? "").Split(' ').Contains(cls))
    ];

    /// <summary>The steps and the heads, apart from the key, which shows a sample of each.</summary>
    private static XElement Steps(string json = Turn) => In("seq-drawing", XElement.Parse(Drawn(json))).Single();

    /// <summary>The key, apart from the steps.</summary>
    private static XElement Key() => In("seq-legend").Single();

    private static string Class(XElement shape) => (string)shape.Attribute("class")!;

    /// <summary>The whole picture, byte for byte, so a change to the look is a change someone reads.</summary>
    [Test]
    public void TheTurn_IsDrawnAsItsGolden() => Golden.Match("sequence-look.svg", Drawn());

    /// <summary>
    /// The picture carries its own title, small, on one light, and its looks in order: every picture's, then the
    /// one it shares with the cards, then its own.
    /// </summary>
    [Test]
    public void ThePicture_HasItsOwnTitle_AndItsLooksInOrder()
    {
        string svg = Drawn();
        int At(string rule) => svg.IndexOf(rule, StringComparison.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(In("mrq-title").Single().Value, Is.EqualTo("One chat turn"));
            Assert.That(In("mrq-light"), Has.Count.EqualTo(1));
            Assert.That(At(".pic-march"), Is.Positive.And.LessThan(At(".mrq-title")));
            Assert.That(At(".mrq-title"), Is.LessThan(At(".seq-beam")));
        });
    }

    /// <summary>
    /// A participant is a badge filled with its tone, its icon in it and its name under it, with no box. A person
    /// who names no icon wears a person's, and a participant with neither tone nor icon has a quiet, empty badge.
    /// </summary>
    [Test]
    public void AParticipant_IsABadgeInItsTone_AndAName()
    {
        var badges = In("seq-badge", Steps()).Select(Class).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(badges, Is.EqualTo(new[]
            {
                "seq-badge tone-blue", "seq-badge tone-green", "seq-badge tone-violet", "seq-badge tone-red",
                "seq-badge"
            }));
            Assert.That(In("pic-badge-icon", Steps()), Has.Count.EqualTo(4), "the person's too, the plain one's not");
            Assert.That(In("seq-name", Steps()).Select(name => name.Value), Is.EqualTo(new[]
            {
                "You", "louis-agent.api", "AgentEngine", "The model that answers,", "wherever it runs", "A plain one"
            }));
            Assert.That(In("box", Steps()), Is.Empty);
        });
    }

    /// <summary>Each participant has a lifeline, from a node under its name down the picture.</summary>
    [Test]
    public void EachParticipant_HasALifeline_FromANodeUnderItsName()
    {
        var lines = In("seq-life", Steps());
        var names = In("seq-name", Steps());
        double Y(XElement shape, string name = "y") => (double)shape.Attribute(name)!;

        Assert.Multiple(() =>
        {
            Assert.That(lines, Has.Count.EqualTo(5));
            Assert.That(lines.Select(line => Y(line)).Distinct().Single(), Is.GreaterThan(names.Max(name => Y(name))));
            Assert.That(In("mrq-node", Steps()).Count(node => Y(node, "cy") == Y(lines[0])), Is.EqualTo(5));
        });
    }

    /// <summary>
    /// A call has its beam with a glow, its dots, a node where it leaves and a blue arrowhead where it arrives.
    /// An answer is a dashed line with a plain arrowhead and a quiet node, and has no beam and no dots.
    /// </summary>
    [Test]
    public void ACall_HasItsBeamItsNodeAndItsDots_AndAnAnswerNone()
    {
        var steps = Steps();
        var beams = In("seq-beam", steps);

        Assert.Multiple(() =>
        {
            Assert.That(beams.Count(beam => !Class(beam).Contains("seq-beam-glow")), Is.EqualTo(5), "straight calls");
            Assert.That(beams.Count(beam => Class(beam).Contains("seq-beam-glow")), Is.EqualTo(5));
            Assert.That(In("pic-march", steps), Has.Count.EqualTo(7), "five straight calls and two to oneself");
            Assert.That(In("seq-tip", steps).Select(tip => (string)tip.Attribute("marker-end")!),
                Is.EqualTo(Enumerable.Repeat("url(#arrow-blue)", 5)));
            Assert.That(In("mrq-node", steps), Has.Count.EqualTo(5 + 7), "the heads' and the calls'");

            var answers = In("seq-answer", steps);
            Assert.That(answers, Has.Count.EqualTo(3));
            Assert.That(answers.Select(answer => (string)answer.Attribute("marker-end")!),
                Is.All.EqualTo("url(#arrow)"));
            Assert.That(In("seq-quiet-node", steps), Has.Count.EqualTo(3));
            Assert.That(In("seq-call-words", steps).Select(words => words.Value), Does.Contain("Record(usage)"));
            Assert.That(In("seq-answer-words", steps).Select(words => words.Value), Is.EqualTo(new[]
            {
                "text, or a tool call", "the text", "SSE: text, tool calls, cost"
            }));
        });
    }

    /// <summary>
    /// A call to the left runs its beam the other way, so violet stays at its sender: the one such call here has
    /// its beam and its glow marked, and the four to the right have not.
    /// </summary>
    [Test]
    public void ACallToTheLeft_RunsItsBeamTheOtherWay()
    {
        var beams = In("seq-beam", Steps());
        var back = beams.Where(beam => Class(beam).Split(' ').Contains("back")).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(back, Has.Count.EqualTo(2), "its beam and its glow");
            Assert.That(beams, Has.Count.EqualTo(10));
        });
    }

    /// <summary>
    /// A call a participant makes to itself leaves its lifeline, turns and comes back: a violet line with an
    /// arrowhead, and its words beside the turn, starting there.
    /// </summary>
    [Test]
    public void ACallToOneself_Turns_WithItsWordsBesideIt()
    {
        var steps = Steps();
        var turns = In("seq-turn", steps);
        var words = In("seq-call-words", steps).Where(text => (string)text.Attribute("text-anchor")! == "start");

        Assert.Multiple(() =>
        {
            Assert.That(turns, Has.Count.EqualTo(2));
            Assert.That(turns.Select(turn => (string)turn.Attribute("marker-end")!),
                Is.All.EqualTo("url(#arrow-violet)"));
            Assert.That(words.Select(text => text.Value), Is.EqualTo(new[] { "run the tool", "summarise it" }));
        });
    }

    /// <summary>A message's words are drawn on the lines the layout broke them into, every word.</summary>
    [Test]
    public void ALongMessage_IsDrawnOnItsLines()
    {
        var words = In("seq-call-words", Steps()).Select(text => text.Value).ToList();

        const string message = "RunTurnAsync, with the whole history of the chat and its tools";
        Assert.That(string.Join(" ", words), Does.Contain(message));
        Assert.That(words, Has.None.EqualTo(message), "over two lines");
    }

    /// <summary>
    /// A note is glass over a cover that hides the lifelines behind it. A block is a glass frame with the accent
    /// along its top, its kind in a pill and its label beside it, and each other case under a dashed divider with
    /// "else" in a quiet pill.
    /// </summary>
    [Test]
    public void ANoteIsGlass_AndABlockAGlassFrameWithItsKindInAPill()
    {
        var steps = Steps();

        Assert.Multiple(() =>
        {
            Assert.That(In("pic-text", steps).Single().Value,
                Is.EqualTo("every request is measured in the ledger"));
            Assert.That(In("mrq-glass", steps), Has.Count.EqualTo(4), "three frames and the note");
            Assert.That(In("seq-cover", steps), Has.Count.EqualTo(4));
            Assert.That(In("mrq-edge", steps), Has.Count.EqualTo(3), "a frame has the accent, a note has not");
            Assert.That(In("seq-kind", steps), Has.Count.EqualTo(3));
            Assert.That(In("mrq-code", steps).Select(kind => kind.Value), Is.EqualTo(new[] { "loop", "alt", "opt" }));
            Assert.That(In("seq-tab", steps).Select(label => label.Value), Is.EqualTo(new[]
            {
                "up to 10 tool rounds", "a tool call", "the answer", "its result is too large"
            }));
            Assert.That(In("seq-divide", steps), Has.Count.EqualTo(1));
            Assert.That(In("seq-else", steps), Has.Count.EqualTo(1));
            Assert.That(In("seq-else-word", steps).Single().Value, Is.EqualTo("else"));
        });
    }

    /// <summary>
    /// A numbered picture puts each step's number in a ring where its line starts, a quiet ring for an answer, in
    /// the order the steps happen. A picture that isn't numbered has no ring.
    /// </summary>
    [Test]
    public void ANumberedPicture_PutsEachNumberInARing()
    {
        var numbered = Steps(Turn.Replace("\"numbered\": false", "\"numbered\": true"));
        var rings = In("seq-ring", numbered);

        Assert.Multiple(() =>
        {
            Assert.That(In("seq-count", numbered).Select(count => count.Value),
                Is.EqualTo(Enumerable.Range(1, 10).Select(number => $"{number}")));
            Assert.That(rings, Has.Count.EqualTo(10));
            Assert.That(rings.Count(ring => Class(ring).Contains("quiet")), Is.EqualTo(3), "the answers'");
            Assert.That(In("mrq-node", numbered).Except(rings).ToList(), Has.Count.EqualTo(5), "the heads'");
            Assert.That(rings.Select(Class), Has.All.StartsWith("mrq-node "), "a ring is the look's node");
            Assert.That(In("seq-ring", Steps()), Is.Empty);
        });
    }

    /// <summary>
    /// The key stands under the steps, below a faint rule: its heading, then a sample and its words for each tone
    /// a badge wears, a call, an answer, a note and each kind of block the picture has.
    /// </summary>
    [Test]
    public void TheKey_ShowsASampleOfWhatThePictureHas()
    {
        var key = Key();

        Assert.Multiple(() =>
        {
            Assert.That(In("mrq-rule", key), Has.Count.EqualTo(1));
            Assert.That(In("pic-group", key).Single().Value, Is.EqualTo("How to read it"));
            Assert.That(In("pic-means", key).Select(words => words.Value), Is.EquivalentTo(new[]
            {
                "starts it", "this project", "the framework", "outside the process", "a call", "an answer", "a note",
                "repeats", "only if", "one case or the other"
            }));
            Assert.That(In("seq-badge", key).Select(Class), Is.EquivalentTo(new[]
            {
                "seq-badge tone-blue", "seq-badge tone-green", "seq-badge tone-violet", "seq-badge tone-red"
            }));
            Assert.That(In("seq-beam", key), Has.Count.EqualTo(2), "a call's sample: its beam and its glow");
            Assert.That(In("seq-answer", key), Has.Count.EqualTo(1));
            Assert.That(In("seq-swatch", key), Has.Count.EqualTo(1));
            Assert.That(In("mrq-code", key).Select(kind => kind.Value), Is.EqualTo(new[] { "loop", "opt", "alt" }));
            Assert.That(In("pic-march", key), Is.Empty, "a sample stands still");
        });
    }

    /// <summary>A caption stands under the key, and the picture is taller by its room.</summary>
    [Test]
    public void ACaption_StandsUnderTheKey()
    {
        const string caption = "Only what changed is written.";
        var plain = XElement.Parse(Drawn());
        var captioned = XElement.Parse(Drawn(Turn.Replace("\"title\":", $"\"caption\": \"{caption}\", \"title\":")));
        double Height(XElement svg) => (double)svg.Attribute("height")!;

        Assert.Multiple(() =>
        {
            Assert.That(In("caption", captioned).Single().Value, Is.EqualTo(caption));
            Assert.That(In("caption", plain), Is.Empty);
            Assert.That(Height(captioned), Is.GreaterThan(Height(plain)));
        });
    }

    /// <summary>
    /// A picture is as wide as its own title when that is wider than its participants, so a long title over two
    /// participants isn't cut off, and the key's rule runs across that width.
    /// </summary>
    [Test]
    public void ANarrowPicture_IsAsWideAsItsTitle()
    {
        const string two = """
                           {
                             "kind": "sequence", "title": "Short",
                             "sequence": {
                               "participants": [ { "id": "a", "label": "A" }, { "id": "b", "label": "B" } ],
                               "steps": [ { "step": "message", "from": "a", "to": "b", "text": "ask" } ]
                             }
                           }
                           """;
        string title = string.Join(" ", Enumerable.Repeat("a long title", 8));
        var narrow = XElement.Parse(Drawn(two));
        var wide = XElement.Parse(Drawn(two.Replace("Short", title)));
        double Width(XElement shape) => (double)shape.Attribute("width")!;

        Assert.Multiple(() =>
        {
            Assert.That(Width(wide), Is.GreaterThan(Width(narrow)).And.GreaterThan(title.Length * 6.5));
            Assert.That(Width(In("mrq-rule", wide).Single()), Is.EqualTo(Width(wide) - 48).Within(0.11));
            Assert.That(Width(In("mrq-rule", narrow).Single()), Is.EqualTo(Width(narrow) - 48).Within(0.11));
        });
    }

    /// <summary>The drawing names no colour: every colour is the style's, by a class or a paint.</summary>
    [Test]
    public void TheDrawing_NamesNoColour()
    {
        var shapes = XElement.Parse(Drawn()).Descendants().Where(shape => shape.Name != Svg + "style").ToList();

        Assert.Multiple(() =>
        {
            Assert.That(shapes.SelectMany(shape => shape.Attributes()).Select(attribute => attribute.Name.LocalName),
                Has.None.EqualTo("fill").And.None.EqualTo("stroke").And.None.EqualTo("style"));
            Assert.That(shapes.Where(shape => shape.Name == Svg + "linearGradient")
                    .Select(paint => (string)paint.Attribute("id")!),
                Is.EquivalentTo(new[] { "seq-beam", "seq-beam-back", "seq-life", "mrq-edge", "mrq-glass" }));
        });
    }
}
