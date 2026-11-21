using LouisAgent.Diagrams.Nested.Spec;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Tests.Nested;

/// <summary>
/// F134.7: a nested picture is data like every other kind. Its JSON reads into a root box that holds boxes, and
/// what stands around it. The reader refuses a picture that could not be drawn as meant, saying what to change: an
/// id used twice, a state that names a pin its box can't reach, a neighbour beside a box that isn't there.
/// </summary>
public class NestedReaderTests
{
    /// <summary>What the reader says when it refuses this JSON.</summary>
    private static string Refusal(string json) =>
        Assert.Throws<InvalidDataException>(() => DiagramReader.Read(json))!.Message;

    /// <summary>
    /// A whole picture reads into its tree: the root with what it holds of its own, the boxes inside it with their
    /// pins, and its neighbours with the side they stand on.
    /// </summary>
    [Test]
    public void AComponentDiagram_ReadsIntoItsTree()
    {
        var source = DiagramReader.Read(NestedSamples.Start);
        var components = source.Nested!;
        var deck = components.Root.Within().Single(box => box.Id == "deck");

        Assert.Multiple(() =>
        {
            Assert.That(source.Kind, Is.EqualTo(DiagramKind.Nested));
            Assert.That(components.Root.Name, Is.EqualTo("ShowroomStart"));
            Assert.That(components.Root.State!.Says, Is.EqualTo("which demo is forward"));
            Assert.That(components.Root.State.Up, Is.EqualTo(new[] { "deck.ForwardChanged", "dock.Chosen" }));
            Assert.That(components.Root.Within().Select(box => box.Id),
                Is.EqualTo(new[] { "start", "stage", "deck", "window", "plain", "dock" }), "each before what it holds");
            Assert.That(deck.In, Is.EqualTo(new[] { "Items", "Forward" }));
            Assert.That(deck.Out, Is.EqualTo(new[] { "ForwardChanged" }));
            Assert.That(deck.Tone, Is.EqualTo(DiagramTone.Green));
            Assert.That(deck.Holds!.Select(held => held.Ghost), Is.EqualTo(new[] { false, true }));
            Assert.That(components.Around!.Select(neighbour => neighbour.Side),
                Is.EqualTo(new[] { NeighbourSide.Before, NeighbourSide.Before, NeighbourSide.After }));
            Assert.That(components.Around![2].To, Is.EqualTo("deck"));
            Assert.That(components.Subtitle, Is.EqualTo("LouisAgent.Web, Site / Showroom / Start"));
            Assert.That(components.Root.Aside, Is.EqualTo("Components/Site/Showroom/Start"));
            Assert.That(components.Around.Select(neighbour => neighbour.Heading),
                Is.EqualTo(new[] { "The site's page", "What the start reads", "Runs in the browser" }));
            Assert.That(components.Around[2].Code, Is.EqualTo("watch, bring"));
        });
    }

    /// <summary>A nested picture without its own part is refused as every kind is.</summary>
    [Test]
    public void WithoutItsPart_ItIsRefused() =>
        Assert.That(Refusal("""{ "kind": "nested", "title": "The start" }"""),
            Does.Contain("needs its \"nested\" part"));

    /// <summary>An id names one box or one neighbour: used twice, a state or a neighbour could mean either.</summary>
    [TestCase("\"id\": \"dock\"", "\"id\": \"deck\"", "deck")]
    [TestCase("\"id\": \"script\"", "\"id\": \"stage\"", "stage")]
    public void AnIdUsedTwice_IsRefused(string old, string instead, string id) =>
        Assert.That(Refusal(NestedSamples.StartWith(old, instead)),
            Does.Contain($"'{id}' is used twice"));

    /// <summary>
    /// A state names the pins that carry it as box.Pin. The box must be one its holder holds, a pin handed down
    /// must be in that box's "in", and one said back up in its "out".
    /// </summary>
    [TestCase("\"deck.Forward\"", "\"deck.Sideways\"", "deck.Sideways", "in")]
    [TestCase("\"deck.Forward\"", "\"shelf.Forward\"", "shelf.Forward", "in")]
    [TestCase("\"deck.Forward\"", "\"Forward\"", "Forward", "in")]
    [TestCase("\"dock.Chosen\"", "\"dock.Current\"", "dock.Current", "out")]
    [TestCase("\"stage.Accent\"", "\"page.Featured\"", "page.Featured", "in")]
    public void AStateThatNamesAPinItCannotReach_IsRefused(string old, string instead, string pin, string list)
    {
        string refusal = Refusal(NestedSamples.StartWith(old, instead));

        Assert.That(refusal, Does.Contain($"'{pin}'").And.Contain("'start'").And.Contain($"\"{list}\""));
    }

    /// <summary>A neighbour stands by a box of the picture. One that names a box nothing has is refused.</summary>
    [Test]
    public void ANeighbourBesideABoxThatIsNotThere_IsRefused() =>
        Assert.That(Refusal(NestedSamples.StartWith("\"to\": \"deck\"", "\"to\": \"shelf\"")),
            Does.Contain("'Deck.razor.js'").And.Contain("'shelf'"));

    /// <summary>A misspelt field is refused, as in every kind, so a typo never draws a box without its pins.</summary>
    [Test]
    public void AMisspeltField_IsRefused() =>
        Assert.That(Refusal(NestedSamples.StartWith("\"ghost\": true", "\"gohst\": true")),
            Does.Contain("can't be read"));
}
