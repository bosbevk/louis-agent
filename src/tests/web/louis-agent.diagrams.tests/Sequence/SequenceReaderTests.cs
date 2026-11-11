using LouisAgent.Diagrams.Icons;
using LouisAgent.Diagrams.Sequence.Spec;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Tests.Sequence;

/// <summary>
/// A sequence is data like a flow (CL60.3): its participants, then its steps in order, each step a record of its own
/// kind, read by its <c>step</c> field. The reader refuses a step that names someone the chart doesn't have.
/// </summary>
public class SequenceReaderTests
{
    /// <summary>A program asks the builder for an engine and is answered: one call, one reply.</summary>
    private const string Sequence = """
                                    {
                                      "kind": "sequence",
                                      "title": "Building the engine",
                                      "sequence": {
                                        "participants": [
                                          { "id": "P", "label": "Program.cs", "icon": "terminal", "tone": "blue" },
                                          { "id": "B", "label": "LouisAgentBuilder", "tone": "violet" },
                                          { "id": "you", "label": "You", "actor": true }
                                        ],
                                        "steps": [
                                          { "step": "message", "from": "P", "to": "B", "text": "Build()" },
                                          { "from": "B", "to": "P", "step": "reply", "text": "AgentHostContext" }
                                        ]
                                      }
                                    }
                                    """;

    /// <summary>A sequence reads into its participants and its steps, in order, each step its own kind.</summary>
    [Test]
    public void ASequence_ReadsIntoItsParticipantsAndSteps()
    {
        var sequence = DiagramReader.Read(Sequence).Sequence!;

        Assert.Multiple(() =>
        {
            Assert.That(sequence.Participants.Select(head => (head.Id, head.Icon, head.Tone, head.Actor)),
                Is.EqualTo(new[]
                {
                    ("P", (DiagramIcon?)DiagramIcon.Terminal, DiagramTone.Blue, false),
                    ("B", null, DiagramTone.Violet, false), ("you", null, DiagramTone.Plain, true)
                }));
            Assert.That(sequence.Steps[0], Is.EqualTo(new MessageStep("P", "B", "Build()")));
            Assert.That(sequence.Steps[1], Is.EqualTo(new ReplyStep("B", "P", "AgentHostContext")));
        });
    }

    /// <summary>A step that names someone the chart doesn't have is refused, naming them.</summary>
    [Test]
    public void AStep_NamingNoParticipant_IsRefused()
    {
        string json = Sequence.Replace("\"to\": \"B\"", "\"to\": \"engine\"", StringComparison.Ordinal);

        Assert.That(() => DiagramReader.Read(json),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("'engine'"));
    }

    /// <summary>A sequence with no steps draws nothing worth a picture, so it is refused.</summary>
    [Test]
    public void ASequence_WithNoSteps_IsRefused()
    {
        var source = new DiagramSource(DiagramKind.Sequence, "Empty",
            Sequence: new SequenceSpec([new Participant("P", "Program.cs")], []));

        Assert.That(() => DiagramReader.Read(DiagramWriter.Write(source)),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("step"));
    }

    /// <summary>
    /// Written, a sequence reads back the same, each step naming its kind first and its defaults left out.
    /// </summary>
    [Test]
    public void ASequence_IsWrittenAndReadBackTheSame()
    {
        string json = DiagramWriter.Write(DiagramReader.Read(Sequence));

        Assert.Multiple(() =>
        {
            Assert.That(DiagramWriter.Write(DiagramReader.Read(json)), Is.EqualTo(json));
            Assert.That(json, Does.Contain("\"step\": \"reply\""));
            Assert.That(json, Does.Not.Contain("\"actor\": false"));
        });
    }
}
