using System.Xml.Linq;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Svg.Renderers.Parts.Types;

namespace LouisAgent.Diagrams.Tests.Svg.Renderers.Parts.Types;

/// <summary>
/// A class chart drawn in the cards' look (F134.11): its own title, each type a glass card with a badge that says
/// its kind, each link a line whose colour and ends say its kind, its word in a pill on the line, and the key
/// under it all. The drawing decides no place itself, so these tests are about what is drawn and how it is marked.
/// </summary>
public class ClassesDrawingTests
{
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

    /// <summary>
    /// A host that holds a factory, with every kind of type and of link: an interface and its two implementers,
    /// one of them abstract with a class that is one, a record that is created, an enum that is used twice, once
    /// from far across the chart, a static helper with a long name, and a plain class in the framework's tone.
    /// </summary>
    private const string Chart = """
                                 {
                                   "kind": "classes",
                                   "title": "Speech: the interface design",
                                   "classes": {
                                     "types": [
                                       { "id": "host", "name": "SpeechHost", "tone": "violet",
                                         "members": [ "+Start(options)", "+Stop()" ] },
                                       { "id": "factory", "name": "SpeechClientFactory", "kind": "static",
                                         "members": [ "+Create(options) ISpeechProvider" ] },
                                       { "id": "provider", "name": "ISpeechProvider", "kind": "interface, step 1",
                                         "members": [ "+Name", "+Listen(stream) Transcript" ] },
                                       { "id": "base", "name": "SpeechProviderBase", "kind": "abstract" },
                                       { "id": "whisper", "name": "WhisperSpeechProvider" },
                                       { "id": "transcript", "name": "Transcript", "kind": "record",
                                         "members": [ "+Text", "+Seconds" ] },
                                       { "id": "name", "name": "SpeechProviderName", "kind": "enum" },
                                       { "id": "helper",
                                         "name": "SpeechStreamReaderForEveryProvider, SpeechStreamWriter",
                                         "kind": "static, two helpers" }
                                     ],
                                     "relations": [
                                       { "from": "host", "to": "factory", "kind": "holds" },
                                       { "from": "factory", "to": "provider", "kind": "creates", "word": "picks one" },
                                       { "from": "base", "to": "provider", "kind": "implements" },
                                       { "from": "whisper", "to": "base", "kind": "inherits" },
                                       { "from": "whisper", "to": "transcript", "kind": "creates" },
                                       { "from": "provider", "to": "name", "kind": "uses" },
                                       { "from": "whisper", "to": "helper", "kind": "uses", "word": "reads with" },
                                       { "from": "host", "to": "name", "kind": "uses" }
                                     ]
                                   }
                                 }
                                 """;

    private static string Drawn(string json = Chart) => ClassesDrawing.Of(DiagramReader.Read(json));

    /// <summary>The shapes drawn in a class, anywhere in a drawing or under one of its shapes.</summary>
    private static List<XElement> In(string cls, XElement? under = null) =>
    [
        .. (under ?? XElement.Parse(Drawn())).Descendants().Where(shape =>
            ((string?)shape.Attribute("class") ?? "").Split(' ').Contains(cls))
    ];

    /// <summary>The types and their links, apart from the key, which shows a sample of each.</summary>
    private static XElement Types(string json = Chart) => In("cls-drawing", XElement.Parse(Drawn(json))).Single();

    /// <summary>The key, apart from the types.</summary>
    private static XElement Key() => In("cls-legend").Single();

    private static string Class(XElement shape) => (string)shape.Attribute("class")!;

    /// <summary>The one line of a kind, with what it starts and ends in.</summary>
    private static (string? Start, string? End) Ends(string kind, XElement under)
    {
        var line = In("cls-line", under).First(shape => Class(shape) == $"cls-line {kind}");
        return ((string?)line.Attribute("marker-start"), (string?)line.Attribute("marker-end"));
    }

    /// <summary>The whole picture, byte for byte, so a change to the look is a change someone reads.</summary>
    [Test]
    public void TheChart_IsDrawnAsItsGolden() => Golden.Match("classes-look.svg", Drawn());

    /// <summary>
    /// The picture carries its own title, small, on one light, and its looks in order: every picture's, then the
    /// one it shares with the cards and the sequence, then its own.
    /// </summary>
    [Test]
    public void ThePicture_HasItsOwnTitle_AndItsLooksInOrder()
    {
        string svg = Drawn();
        int At(string rule) => svg.IndexOf(rule, StringComparison.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(In("mrq-title").Single().Value, Is.EqualTo("Speech: the interface design"));
            Assert.That(In("mrq-light"), Has.Count.EqualTo(1));
            Assert.That(At(".pic-march"), Is.Positive.And.LessThan(At(".mrq-title")));
            Assert.That(At(".mrq-title"), Is.LessThan(At(".cls-line")));
        });
    }

    /// <summary>
    /// A type is a glass card: a badge in its tone with a letter for its kind, its name, its kind under the name,
    /// and its members under a divider, without the plus a member is written with.
    /// </summary>
    [Test]
    public void AType_IsAGlassCard_WithABadgeThatSaysItsKind()
    {
        var types = Types();

        Assert.Multiple(() =>
        {
            Assert.That(In("mrq-glass", types), Has.Count.EqualTo(8));
            Assert.That(In("cls-mark", types).Select(mark => mark.Value),
                Is.EquivalentTo(new[] { "C", "S", "I", "A", "C", "R", "E", "S" }));
            Assert.That(In("cls-badge", types).Select(Class).Order(), Is.EqualTo(new[]
            {
                "cls-badge tone-green", "cls-badge tone-green", "cls-badge tone-green", "cls-badge tone-green",
                "cls-badge tone-teal", "cls-badge tone-teal", "cls-badge tone-teal", "cls-badge tone-violet"
            }), "the kinds' tones, and the one a type names");
            Assert.That(In("cls-kind", types).Select(kind => kind.Value), Does.Contain("interface, step 1"));
            Assert.That(In("cls-divide", types), Has.Count.EqualTo(4), "the four types with members");
            Assert.That(In("cls-member", types).Select(member => member.Value),
                Does.Contain("Start(options)").And.Contain("Listen(stream) Transcript").And.None.StartsWith("+"));
            Assert.That(In("box", types), Is.Empty);
        });
    }

    /// <summary>
    /// A contract, an interface, a record or an enum, wears the accent along its top, and the other types don't.
    /// </summary>
    [Test]
    public void AContract_WearsTheAccentAlongItsTop() =>
        Assert.That(In("mrq-edge", Types()), Has.Count.EqualTo(3), "the interface, the record and the enum");

    /// <summary>A long name is drawn on the lines it was wrapped to, every word of it.</summary>
    [Test]
    public void ALongName_IsDrawnOnItsLines()
    {
        var names = In("cls-name", Types()).Select(name => name.Value).ToList();

        const string name = "SpeechStreamReaderForEveryProvider, SpeechStreamWriter";
        Assert.That(string.Join(" ", names), Does.Contain(name));
        Assert.That(names, Has.None.EqualTo(name), "over two lines");
    }

    /// <summary>
    /// A link's kind is in its line and its ends: a holds starts in a diamond and ends in an arrowhead, an is a
    /// and an implements end in a hollow triangle, a creates in a blue arrowhead and a uses in an open one.
    /// </summary>
    [Test]
    public void ALinksKind_IsInItsLineAndItsEnds()
    {
        var types = Types();

        Assert.Multiple(() =>
        {
            Assert.That(In("cls-line", types).Select(Class).Order(), Is.EqualTo(new[]
            {
                "cls-line creates", "cls-line creates", "cls-line holds", "cls-line implements", "cls-line inherits",
                "cls-line uses", "cls-line uses", "cls-line uses"
            }));
            Assert.That(Ends("holds", types), Is.EqualTo(("url(#diamond-violet)", "url(#arrow-violet)")));
            Assert.That(Ends("inherits", types), Is.EqualTo(((string?)null, "url(#triangle-violet)")));
            Assert.That(Ends("implements", types), Is.EqualTo(((string?)null, "url(#triangle-violet)")));
            Assert.That(Ends("creates", types), Is.EqualTo(((string?)null, "url(#arrow-blue)")));
            Assert.That(Ends("uses", types), Is.EqualTo(((string?)null, "url(#open)")));
        });
    }

    /// <summary>
    /// A holds, an is a and a creates carry running dots. An implements and a uses stand still: four lines move
    /// here, and three don't.
    /// </summary>
    [Test]
    public void TheStructureAndACreates_Move_AndAUseStandsStill() =>
        Assert.That(In("pic-march", Types()), Has.Count.EqualTo(4), "one holds, one is a, two creates");

    /// <summary>
    /// A link's word stands in a pill on its line, in the line's kind, and a use's word is quiet. A link with no
    /// word of its own says its kind.
    /// </summary>
    [Test]
    public void ALinksWord_StandsInAPillOfItsKind()
    {
        var types = Types();

        Assert.Multiple(() =>
        {
            Assert.That(In("cls-word", types).Select(word => word.Value),
                Is.EquivalentTo(new[] { "holds", "picks one", "implements", "is a", "creates", "uses", "reads with" }));
            Assert.That(In("cls-chip", types).Select(Class),
                Does.Contain("cls-chip holds").And.Contain("cls-chip uses"));
            Assert.That(In("cls-word", types).Where(word => Class(word).Contains("quiet")).Select(word => word.Value),
                Is.EquivalentTo(new[] { "uses", "reads with" }));
        });
    }

    /// <summary>
    /// A holds' word stands clear of the diamond its line starts in: a flow puts a word just after a line's
    /// start, which is where the diamond is, so the pill is moved along the line past it.
    /// </summary>
    [Test]
    public void AHoldsWord_StandsClearOfItsDiamond()
    {
        var types = Types();
        string path = (string)In("cls-line", types).Single(line => Class(line) == "cls-line holds").Attribute("d")!;
        double start = double.Parse(path.Split(' ')[1], System.Globalization.CultureInfo.InvariantCulture);
        var pill = In("cls-chip", types).Single(chip => Class(chip) == "cls-chip holds");

        Assert.That((double)pill.Attribute("x")!, Is.GreaterThanOrEqualTo(start + 14), "the diamond is 14 long");
    }

    /// <summary>
    /// The key stands under the types, below a faint rule: its heading, then a sample and its words for each tone
    /// a badge wears, each letter and each kind of link. A sample stands still.
    /// </summary>
    [Test]
    public void TheKey_ShowsASampleOfWhatThePictureHas()
    {
        var key = Key();

        Assert.Multiple(() =>
        {
            Assert.That(In("cls-rule", key), Has.Count.EqualTo(1));
            Assert.That(In("pic-group", key).Single().Value, Is.EqualTo("How to read it"));
            Assert.That(In("cls-key", key).Select(words => words.Value), Is.EqualTo(new[]
            {
                "this project", "the framework", "contracts and state", "an interface", "a record", "an enum",
                "abstract", "static", "a class", "holds", "is a", "implements", "creates", "uses"
            }));
            Assert.That(In("cls-key-letter", key).Select(letter => letter.Value),
                Is.EqualTo(new[] { "I", "R", "E", "A", "S", "C" }));
            Assert.That(In("cls-line", key), Has.Count.EqualTo(5));
            Assert.That(In("pic-march", key), Is.Empty, "a sample stands still");
        });
    }

    /// <summary>A caption stands under the key, and the picture is taller by its room.</summary>
    [Test]
    public void ACaption_StandsUnderTheKey()
    {
        const string caption = "Speech is a provider like a model.";
        var plain = XElement.Parse(Drawn());
        var captioned = XElement.Parse(Drawn(Chart.Replace("\"title\":", $"\"caption\": \"{caption}\", \"title\":")));
        double Height(XElement svg) => (double)svg.Attribute("height")!;

        Assert.Multiple(() =>
        {
            Assert.That(In("caption", captioned).Single().Value, Is.EqualTo(caption));
            Assert.That(In("caption", plain), Is.Empty);
            Assert.That(Height(captioned), Is.GreaterThan(Height(plain)));
        });
    }

    /// <summary>The drawing names no colour: every colour is the style's, by a class or a paint.</summary>
    [Test]
    public void TheDrawing_NamesNoColour()
    {
        var shapes = XElement.Parse(Drawn()).Descendants().Where(shape => shape.Name != Svg + "style").ToList();

        Assert.That(shapes.SelectMany(shape => shape.Attributes()).Select(attribute => attribute.Name.LocalName),
            Has.None.EqualTo("fill").And.None.EqualTo("stroke").And.None.EqualTo("style"));
    }
}
