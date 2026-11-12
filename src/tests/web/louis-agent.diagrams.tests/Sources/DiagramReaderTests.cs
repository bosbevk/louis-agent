using LouisAgent.Diagrams.Cards;
using LouisAgent.Diagrams.Cards.Links;
using LouisAgent.Diagrams.Icons;
using LouisAgent.Diagrams.Sources;

namespace LouisAgent.Diagrams.Tests.Sources;

/// <summary>
/// A diagram's source is a JSON file beside its doc (CL60.1, Louis's pick): the reader turns it into records, and
/// refuses a file it can't draw, saying what is wrong, so a typo never becomes a picture with a hole in it.
/// </summary>
public class DiagramReaderTests
{
    /// <summary>A flow: boxes in columns and the arrows between them, as the site's diagrams are.</summary>
    private const string Flow = """
                                {
                                  "kind": "flow",
                                  "title": "A turn",
                                  "flow": {
                                    "nodes": [
                                      { "id": "host", "label": "A host", "column": 0 },
                                      { "id": "engine", "label": "The engine", "note": "the loop", "column": 1 }
                                    ],
                                    "edges": [ { "from": "host", "to": "engine", "word": "runs" } ]
                                  }
                                }
                                """;

    /// <summary>Cards: Louis's figure, the engine on top and the identities and packs under it.</summary>
    private const string Cards = """
                                 {
                                   "kind": "cards",
                                   "title": "Engine, identities and packs",
                                   "caption": "The separation ADR 0027 and ADR 0028 document.",
                                   "cards": {
                                     "rows": [
                                       { "cards": [
                                         { "id": "engine", "icon": "chip", "title": "Engine",
                                           "code": "louis-agent.core", "text": "The runtime and the tool loop." }
                                       ] },
                                       { "cards": [
                                         { "id": "identities", "icon": "user", "title": "Identities",
                                           "text": "Who the agent is." },
                                         { "id": "packs", "icon": "box", "title": "Packs",
                                           "text": "What the agent can do." }
                                       ] }
                                     ],
                                     "links": [
                                       { "from": "engine", "to": "identities", "kind": "both" },
                                       { "from": "engine", "to": "packs", "kind": "both" }
                                     ]
                                   }
                                 }
                                 """;

    /// <summary>A flow file reads into its boxes and arrows.</summary>
    [Test]
    public void AFlow_ReadsIntoItsBoxesAndArrows()
    {
        var source = DiagramReader.Read(Flow);

        Assert.Multiple(() =>
        {
            Assert.That((source.Kind, source.Title), Is.EqualTo((DiagramKind.Flow, "A turn")));
            Assert.That(source.Flow!.Nodes.Select(node => (node.Id, node.Column)),
                Is.EqualTo(new[] { ("host", 0), ("engine", 1) }));
            Assert.That(source.Flow.Nodes[1].Note, Is.EqualTo("the loop"));
            Assert.That(source.Flow.Edges.Single().Word, Is.EqualTo("runs"));
        });
    }

    /// <summary>A cards file reads into its rows, its cards with their icons, and its links.</summary>
    [Test]
    public void Cards_ReadIntoRowsCardsAndLinks()
    {
        var source = DiagramReader.Read(Cards);

        Assert.Multiple(() =>
        {
            Assert.That(source.Kind, Is.EqualTo(DiagramKind.Cards));
            Assert.That(source.Caption, Does.StartWith("The separation"));
            Assert.That(source.Cards!.Rows.Select(row => row.Cards.Count), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(source.Cards.Rows[0].Cards[0], Is.EqualTo(new Card("engine", DiagramIcon.Chip, "Engine",
                "louis-agent.core", "The runtime and the tool loop.")));
            Assert.That(source.Cards.Links.Select(link => link.Kind), Has.All.EqualTo(CardLinkKind.Both));
        });
    }

    /// <summary>A card is plain unless it names its look, and reads the look it names.</summary>
    [Test]
    public void ACard_ReadsItsLook_PlainWhenItNamesNone()
    {
        var source = DiagramReader.Read(Cards.Replace("\"title\": \"Engine\",",
            "\"title\": \"Engine\", \"look\": \"highlight\","));

        var cards = source.Cards!.Rows.SelectMany(row => row.Cards).ToList();
        Assert.That(cards.Select(card => card.Look),
            Is.EqualTo(new[] { CardLook.Highlight, CardLook.Plain, CardLook.Plain }));
    }

    /// <summary>A kind the renderer doesn't draw is refused, naming the kinds it does.</summary>
    [Test]
    public void AnUnknownKind_IsRefused_NamingTheKinds() =>
        Assert.That(() => DiagramReader.Read(Flow.Replace("\"flow\",", "\"pie\",")),
            Throws.TypeOf<InvalidDataException>().With.Message.Contain("pie").And.Message.Contain("flow, cards"));

    /// <summary>An arrow to a box the diagram doesn't have is refused, naming it.</summary>
    [Test]
    public void AnArrowToAMissingBox_IsRefused_NamingIt() =>
        Assert.That(() => DiagramReader.Read(Flow.Replace("\"to\": \"engine\"", "\"to\": \"engin\"")),
            Throws.TypeOf<InvalidDataException>().With.Message.Contain("engin"));

    /// <summary>A link to a card the diagram doesn't have is refused, naming it.</summary>
    [Test]
    public void ALinkToAMissingCard_IsRefused_NamingIt() =>
        Assert.That(() => DiagramReader.Read(Cards.Replace("\"to\": \"packs\"", "\"to\": \"pack\"")),
            Throws.TypeOf<InvalidDataException>().With.Message.Contain("pack"));

    /// <summary>A kind without its own part (a cards diagram with no cards) is refused.</summary>
    [Test]
    public void AKindWithoutItsPart_IsRefused() =>
        Assert.That(() => DiagramReader.Read("""{ "kind": "cards", "title": "Empty" }"""),
            Throws.TypeOf<InvalidDataException>().With.Message.Contain("cards"));

    /// <summary>
    /// A diagram without a title is refused, since the title is what a reader of the picture is told.
    /// </summary>
    [Test]
    public void ADiagramWithoutATitle_IsRefused() =>
        Assert.That(() => DiagramReader.Read(Flow.Replace("\"title\": \"A turn\",", "")),
            Throws.TypeOf<InvalidDataException>().With.Message.Contain("title"));

    /// <summary>
    /// A misspelt field is refused rather than dropped, so a typo can't leave a box without its note.
    /// </summary>
    [Test]
    public void AMisspeltField_IsRefused() =>
        Assert.That(() => DiagramReader.Read(Flow.Replace("\"note\"", "\"nte\"")),
            Throws.TypeOf<InvalidDataException>().With.Message.Contain("nte"));
}
