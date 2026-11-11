using LouisAgent.Diagrams.Classes.Spec;
using LouisAgent.Diagrams.Flow.Spec;
using LouisAgent.Diagrams.Sources;
using LouisAgent.DocDiagrams.Mermaid;
using LouisAgent.DocDiagrams.Mermaid.Readers;

namespace LouisAgent.DocDiagrams.Tests.Mermaid;

/// <summary>
/// The one-time import of a docs class diagram (CL60.4): every form the docs write read into a class chart, so the
/// block can be swapped for its picture and Mermaid kept nowhere.
/// </summary>
public class MermaidClassReaderTests
{
    /// <summary>The class chart a block reads into.</summary>
    private static ClassesSpec Read(string block) => MermaidClassReader.Read(block, "A chart").Classes!;

    /// <summary>A class written over several lines reads its kind and members, its generics as C# writes them.</summary>
    [Test]
    public void AClassOverLines_ReadsItsKindAndMembers()
    {
        var type = Read("""
                        classDiagram
                            class TranscriptionEndpoints {
                                <<api, step 3>>
                                Transcribe(id, IFormFile audio) Results~Ok~TranscriptionResult~, BadRequest~
                                GetTextAsync(Stream audio) Task~SpeechToTextResponse~
                            }
                        """).Types.Single();

        Assert.Multiple(() =>
        {
            Assert.That((type.Id, type.Name, type.Kind),
                Is.EqualTo(("TranscriptionEndpoints", "TranscriptionEndpoints", (string?)"api, step 3")));
            Assert.That(type.Members, Is.EqualTo(new[]
            {
                "Transcribe(id, IFormFile audio) Results<Ok<TranscriptionResult>, BadRequest>",
                "GetTextAsync(Stream audio) Task<SpeechToTextResponse>"
            }));
        });
    }

    /// <summary>
    /// A class written on one line splits its members where each starts with its visibility, and a generic class
    /// keeps its id plain and its name as C# writes it.
    /// </summary>
    [Test]
    public void AClassOnOneLine_SplitsItsMembers()
    {
        var types = Read("""
                         classDiagram
                             class ITriageRecorder { <<interface>> +SaveAsync(step, ct) +ReadAsync(runId) }
                             class IResultCheck~T~ { <<interface>> F39 A5 }
                         """).Types;

        Assert.Multiple(() =>
        {
            Assert.That(types[0].Members, Is.EqualTo(new[] { "+SaveAsync(step, ct)", "+ReadAsync(runId)" }));
            Assert.That((types[1].Id, types[1].Name, types[1].Kind),
                Is.EqualTo(("IResultCheck", "IResultCheck<T>", (string?)"interface")));
            Assert.That(types[1].Members, Is.EqualTo(new[] { "F39 A5" }));
        });
    }

    /// <summary>Each arrow reads as its relation, the arrow's open end the one it points from, with its label.</summary>
    [Test]
    public void EachArrow_ReadsAsItsRelation()
    {
        var relations = Read("""
                             classDiagram
                                 class A
                                 A <|-- B
                                 A <|.. C
                                 D o-- A : a registry
                                 D --> E : reads
                                 E ..> A : asks
                             """).Relations;

        Assert.That(relations, Is.EqualTo(new[]
        {
            new ClassRelation("B", "A", RelationKind.Inherits), new ClassRelation("C", "A", RelationKind.Implements),
            new ClassRelation("D", "A", RelationKind.Holds, "a registry"),
            new ClassRelation("D", "E", RelationKind.Holds, "reads"), new ClassRelation("E", "A", RelationKind.Uses, "asks")
        }));
    }

    /// <summary>A type named only in a relation joins the chart where it first appears, and a direction is kept.</summary>
    [Test]
    public void ATypeNamedOnlyInARelation_Joins_AndADirectionIsKept()
    {
        var classes = Read("classDiagram\n    direction TB\n    class A\n    A <|-- B");

        Assert.Multiple(() =>
        {
            Assert.That(classes.Types.Select(type => type.Id), Is.EqualTo(new[] { "A", "B" }));
            Assert.That(classes.Direction, Is.EqualTo(FlowDirection.Down));
        });
    }

    /// <summary>A line the reader doesn't know is refused, naming it, rather than left out of the picture.</summary>
    [Test]
    public void AnUnknownLine_IsRefused() =>
        Assert.That(() => Read("classDiagram\n    note for A \"why\""),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("note for A"));

    /// <summary>Importing a doc writes its class chart as its own kind.</summary>
    [Test]
    public void ImportingADoc_WritesItsClassChart()
    {
        string folder = Directory.CreateTempSubdirectory("louis-agent-import-").FullName;
        string doc = Path.Combine(folder, "louis-agent.cli.md");
        File.WriteAllText(doc, "# louis-agent.cli\n\n## Types\n\n```mermaid\nclassDiagram\n    A <|-- B\n```\n");

        var json = MermaidImport.Write(doc).Single().Json;

        Assert.That(DiagramReader.Read(File.ReadAllText(json)).Kind, Is.EqualTo(DiagramKind.Classes));
    }
}
