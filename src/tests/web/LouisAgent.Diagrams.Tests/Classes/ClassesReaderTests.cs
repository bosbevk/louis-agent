using LouisAgent.Diagrams.Classes.Spec;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Tests.Classes;

/// <summary>
/// A class chart is data like a flow (CL60.4): its types with their kind and members, and the relations between
/// them. The reader refuses a relation that names a type the chart doesn't have.
/// </summary>
public class ClassesReaderTests
{
    /// <summary>A speech provider, its factory and the one implementation: two relations.</summary>
    private const string Classes = """
                                   {
                                     "kind": "classes",
                                     "title": "Speech",
                                     "classes": {
                                       "types": [
                                         { "id": "provider", "name": "ISpeechProvider", "kind": "interface",
                                           "members": [ "SpeechProviderName Name", "Create(SpeechOptions options)" ],
                                           "tone": "violet" },
                                         { "id": "whisper", "name": "WhisperSpeechProvider", "kind": "class" },
                                         { "id": "factory", "name": "SpeechClientFactory" }
                                       ],
                                       "relations": [
                                         { "from": "whisper", "to": "provider", "kind": "implements" },
                                         { "from": "factory", "to": "provider", "kind": "holds", "word": "a registry" }
                                       ]
                                     }
                                   }
                                   """;

    /// <summary>A class chart reads into its types, with their kind and members, and its relations.</summary>
    [Test]
    public void AClassChart_ReadsIntoItsTypesAndRelations()
    {
        var classes = DiagramReader.Read(Classes).Classes!;

        Assert.Multiple(() =>
        {
            Assert.That(classes.Types.Select(type => (type.Id, type.Name, type.Kind)), Is.EqualTo(new[]
            {
                ("provider", "ISpeechProvider", (string?)"interface"), ("whisper", "WhisperSpeechProvider", "class"),
                ("factory", "SpeechClientFactory", null)
            }));
            Assert.That(classes.Types[0].Members, Is.EqualTo(new[] { "SpeechProviderName Name", "Create(SpeechOptions options)" }));
            Assert.That(classes.Types[0].Tone, Is.EqualTo(DiagramTone.Violet));
            Assert.That(classes.Relations.Select(relation => (relation.Kind, relation.Word)),
                Is.EqualTo(new[] { (RelationKind.Implements, (string?)null), (RelationKind.Holds, "a registry") }));
        });
    }

    /// <summary>A relation that names a type the chart doesn't have is refused, naming it.</summary>
    [Test]
    public void ARelation_NamingNoType_IsRefused()
    {
        string json = Classes.Replace("\"to\": \"provider\", \"kind\": \"implements\"",
            "\"to\": \"ghost\", \"kind\": \"implements\"", StringComparison.Ordinal);

        Assert.That(() => DiagramReader.Read(json),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("'ghost'"));
    }

    /// <summary>Written, a class chart reads back the same, every relation's kind written, the first kind too.</summary>
    [Test]
    public void AClassChart_IsWrittenAndReadBackTheSame()
    {
        var source = new DiagramSource(DiagramKind.Classes, "Inherits",
            Classes: new ClassesSpec([new ClassType("a", "A"), new ClassType("b", "B")],
                [new ClassRelation("b", "a", RelationKind.Inherits)]));
        string json = DiagramWriter.Write(source);

        Assert.Multiple(() =>
        {
            Assert.That(DiagramWriter.Write(DiagramReader.Read(json)), Is.EqualTo(json));
            Assert.That(json, Does.Contain("\"kind\": \"inherits\""));
        });
    }
}
