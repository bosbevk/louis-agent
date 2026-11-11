using LouisAgent.Diagrams.Sequence.Spec;
using LouisAgent.Diagrams.Sequence.Spec.Steps;
using LouisAgent.Diagrams.Sources;
using LouisAgent.DocDiagrams.Mermaid;
using LouisAgent.DocDiagrams.Mermaid.Readers;

namespace LouisAgent.DocDiagrams.Tests.Mermaid;

/// <summary>
/// The one-time import of a docs sequence diagram (CL60.3): every form the docs write read into a sequence, so the
/// block can be swapped for its picture and Mermaid kept nowhere.
/// </summary>
public class MermaidSequenceReaderTests
{
    /// <summary>The sequence a block reads into.</summary>
    private static SequenceSpec Read(string block) => MermaidSequenceReader.Read(block, "A chart").Sequence!;

    /// <summary>Participants and actors read with their names, an actor marked as a person.</summary>
    [Test]
    public void ParticipantsAndActors_ReadWithTheirNames()
    {
        var sequence = Read("""
                            sequenceDiagram
                                actor V as Visitor
                                participant P as Program.cs
                                participant You
                                V->>P: hi
                            """);

        Assert.That(sequence.Participants, Is.EqualTo(new[]
        {
            new Participant("V", "Visitor", Actor: true), new Participant("P", "Program.cs"),
            new Participant("You", "You")
        }));
    }

    /// <summary>A call, a reply and a plain line read in order, each with its words.</summary>
    [Test]
    public void CallsAndReplies_ReadInOrder()
    {
        var sequence = Read("""
                            sequenceDiagram
                                P->>S: AddLouisAgent(agent => …)
                                S-->>P: the context
                                P->S: Build()
                            """);

        Assert.That(sequence.Steps, Is.EqualTo(new SequenceStep[]
        {
            new MessageStep("P", "S", "AddLouisAgent(agent => …)"), new ReplyStep("S", "P", "the context"),
            new MessageStep("P", "S", "Build()")
        }));
    }

    /// <summary>
    /// A message keeps its generics, since Mermaid reads no tags in a message; a break and a coded character read
    /// as themselves.
    /// </summary>
    [Test]
    public void AMessage_KeepsItsGenerics_AndReadsItsCodes()
    {
        var sequence = Read("""
                            sequenceDiagram
                                B->>B: AddPack<CalculatorPack>()#59; checked<br/>every name
                            """);

        Assert.That(((MessageStep)sequence.Steps.Single()).Text,
            Is.EqualTo("AddPack<CalculatorPack>(); checked\nevery name"));
    }

    /// <summary>Someone named only in a message joins the chart where they first appear.</summary>
    [Test]
    public void AParticipantNamedOnlyInAMessage_JoinsInOrder()
    {
        var sequence = Read("""
                            sequenceDiagram
                                participant B as the builder
                                A->>B: go
                                B->>C: on
                            """);

        Assert.That(sequence.Participants.Select(head => head.Id), Is.EqualTo(new[] { "B", "A", "C" }));
    }

    /// <summary>Notes, numbers and nested blocks read into their steps, each block holding its own.</summary>
    [Test]
    public void NotesNumbersAndBlocks_ReadIntoTheirSteps()
    {
        var sequence = Read("""
                            sequenceDiagram
                                autonumber
                                Note over A,B: a turn runs
                                loop up to 10 tool rounds
                                    A->>B: call
                                    alt it works
                                        B-->>A: a result
                                    else it fails
                                        Note right of B: the reason
                                    end
                                end
                                opt a name nobody knows
                                    A->>A: refuse it
                                end
                            """);

        Assert.Multiple(() =>
        {
            Assert.That(sequence.Numbered, Is.True);
            var note = (NoteStep)sequence.Steps[0];
            Assert.That((string.Join(',', note.Over), note.Text), Is.EqualTo(("A,B", "a turn runs")));
            var loop = (BlockStep)sequence.Steps[1];
            Assert.That((loop.Block, loop.Label, loop.Steps.Count), Is.EqualTo((BlockKind.Loop, "up to 10 tool rounds", 2)));
            var alt = (BlockStep)loop.Steps[1];
            Assert.That(alt.Else!.Single().Label, Is.EqualTo("it fails"));
            Assert.That(((NoteStep)alt.Else![0].Steps.Single()).Over, Is.EqualTo(new[] { "B" }));
            Assert.That(((BlockStep)sequence.Steps[2]).Block, Is.EqualTo(BlockKind.Opt));
        });
    }

    /// <summary>A line the reader doesn't know is refused, naming it, rather than left out of the picture.</summary>
    [Test]
    public void AnUnknownLine_IsRefused() =>
        Assert.That(() => Read("sequenceDiagram\n    par at once\n    end"),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("par at once"));

    /// <summary>Importing a doc writes its sequence as well as its flowchart, each as its own kind.</summary>
    [Test]
    public void ImportingADoc_WritesItsSequenceToo()
    {
        string folder = Directory.CreateTempSubdirectory("louis-agent-import-").FullName;
        string doc = Path.Combine(folder, "louis-agent.cli.md");
        File.WriteAllText(doc,
            "# louis-agent.cli\n\n## Parts\n\n```mermaid\nflowchart LR\n    a --> b\n```\n\n" +
            "## A turn\n\n```mermaid\nsequenceDiagram\n    a->>b: hi\n```\n");

        var kinds = MermaidImport.Write(doc).Select(imported => DiagramReader.Read(File.ReadAllText(imported.Json)).Kind);

        Assert.That(kinds, Is.EqualTo(new[] { DiagramKind.Flow, DiagramKind.Sequence }));
    }
}
