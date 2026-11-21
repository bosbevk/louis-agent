using LouisAgent.Choices;
using LouisAgent.Diagrams.Composite.Spec;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Tests.Composite;

/// <summary>
/// F134.13: a composite structure diagram is data like every other kind. Its JSON reads into one class, its parts,
/// its ports and its connectors. The reader refuses a picture that could not be drawn as meant, saying what to
/// change: no part, an id used twice, a port or a connector that names a part the class doesn't have.
/// </summary>
public class CompositeReaderTests
{
    /// <summary>What the reader says when it refuses this JSON.</summary>
    private static string Refusal(string json) =>
        Assert.Throws<InvalidDataException>(() => DiagramReader.Read(json))!.Message;

    /// <summary>
    /// A whole picture reads into its class: its name and words, its parts, its ports and connectors.
    /// </summary>
    [Test]
    public void AComposite_ReadsIntoItsClass()
    {
        var source = DiagramReader.Read(CompositeSamples.Engine);
        var composite = source.Composite!;

        Assert.Multiple(() =>
        {
            Assert.That(source.Kind, Is.EqualTo(DiagramKind.Composite));
            Assert.That((composite.Name, composite.Kind, composite.Aside, composite.Tone),
                Is.EqualTo(("AgentEngine", "class", "LouisAgent.Core, Agent", DiagramTone.Green)));
            Assert.That(composite.Parts.Select(part => part.Id),
                Is.EqualTo(new[] { "turns", "guards", "registry", "commands" }));
            Assert.That(composite.Parts[0], Is.EqualTo(
                new CompositePart("turns", "TurnRunner", "turns", "one turn on the pipeline")));
            Assert.That(composite.Parts[2].Note, Is.Null, "a part may have no note");
            Assert.That(composite.Ports!.Select(port => port.Side),
                Is.EqualTo(new[] { PortSide.In, PortSide.In, PortSide.Out }));
            Assert.That(composite.Ports![2], Is.EqualTo(
                new CompositePort("model", PortSide.Out, "guards", "IChatClient", DiagramTone.Teal)));
            Assert.That(composite.Connectors![0],
                Is.EqualTo(new CompositeConnector("turns", "guards", "the pipeline")));
        });
    }

    /// <summary>A composite without its own part is refused as every kind is.</summary>
    [Test]
    public void WithoutItsPart_ItIsRefused() =>
        Assert.That(Refusal("""{ "kind": "composite", "title": "The engine" }"""),
            Does.Contain("needs its \"composite\" part"));

    /// <summary>A class with no part has no inside to show.</summary>
    [Test]
    public void AClassWithNoPart_IsRefused() =>
        Assert.That(
            Refusal("""{ "kind": "composite", "title": "E", "composite": { "name": "Empty", "parts": [] } }"""),
            Does.Contain("'Empty'").And.Contain("no part"));

    /// <summary>An id names one part: used twice, a port or a connector could mean either.</summary>
    [Test]
    public void AnIdUsedTwice_IsRefused() =>
        Assert.That(Refusal(CompositeSamples.Engine.Replace("\"id\": \"guards\"", "\"id\": \"turns\"")),
            Does.Contain("'turns'").And.Contain("twice"));

    /// <summary>A port passes to a part, or comes from one, that the class has.</summary>
    [Test]
    public void APortThatNamesNoPart_IsRefused() =>
        Assert.That(Refusal(CompositeSamples.Engine.Replace("\"part\": \"commands\"", "\"part\": \"nobody\"")),
            Does.Contain("'typed'").And.Contain("'nobody'"));

    /// <summary>A connector joins two parts the class has, and says which end is astray.</summary>
    [Test]
    public void AConnectorThatNamesNoPart_IsRefused() =>
        Assert.That(
            Refusal(CompositeSamples.Engine.Replace("\"from\": \"guards\", \"to\": \"registry\"",
                "\"from\": \"guards\", \"to\": \"nowhere\"")),
            Does.Contain("'guards'").And.Contain("'nowhere'"));

    /// <summary>The kind is one of the kinds a picture may name, by its own word.</summary>
    [Test]
    public void TheKind_IsNamedComposite() => Assert.That(DiagramKind.Composite.Wire(), Is.EqualTo("composite"));
}
