using LouisAgent.Diagrams.Icons;
using LouisAgent.Diagrams.Sequence.Spec;
using LouisAgent.Diagrams.Sequence.Spec.Steps;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Svg;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Tests.Sequence;

/// <summary>
/// A sequence drawn as one SVG (CL60.3) in the flows' look: heads as toned boxes with an icon badge, dashed
/// lifelines, arrows in their sender's tone, replies dashed, notes on panels and blocks in quiet frames.
/// </summary>
public class SequenceRendererTests
{
    /// <summary>A person asks the API, which runs the engine round its tools and answers, numbered.</summary>
    private static DiagramSource Chat() => new(DiagramKind.Sequence, "One chat turn",
        Sequence: new SequenceSpec(
        [
            new Participant("you", "You", Actor: true, Tone: DiagramTone.Blue),
            new Participant("api", "louis-agent.api", DiagramIcon.Server, DiagramTone.Green),
            new Participant("engine", "AgentEngine", DiagramIcon.Chip, DiagramTone.Violet),
            new Participant("model", "the model", DiagramIcon.Cloud, DiagramTone.Red)
        ],
        [
            new MessageStep("you", "api", "POST /v1/sessions/{id}/messages"),
            new MessageStep("api", "engine", "RunTurnAsync"),
            new BlockStep(BlockKind.Loop, "up to 10 tool rounds",
            [
                new MessageStep("engine", "model", "the request, with the tools"),
                new BlockStep(BlockKind.Alt, "a tool call", [new MessageStep("engine", "engine", "run the tool")],
                    [new ElseCase("the answer", [new ReplyStep("model", "engine", "text")])])
            ]),
            new NoteStep(["api", "engine"], "every request is measured in the ledger"),
            new ReplyStep("api", "you", "SSE: text, tool calls, cost")
        ],
        Numbered: true));

    /// <summary>The drawing matches its golden file, looked at by eye when it was made.</summary>
    [Test]
    public void ASequence_MatchesTheGoldenFile() => Golden.Match("sequence.svg", DiagramRenderers.Render(Chat()));

    /// <summary>Every head, message, note and block is drawn, with its words.</summary>
    [Test]
    public void EveryHeadMessageNoteAndBlock_IsDrawn()
    {
        string svg = DiagramRenderers.Render(Chat());

        Assert.Multiple(() =>
        {
            foreach (string words in new[]
                     {
                         "You", "louis-agent.api", "AgentEngine", "the model", "RunTurnAsync", "run the tool",
                         "every request is measured in the ledger", "loop", "up to 10 tool rounds", "alt",
                         "the answer", ">5<"
                     })
                Assert.That(svg, Does.Contain(words), words);
        });
    }

    /// <summary>A reply runs dashed, in the tone of whoever answers; a call runs solid in the caller's.</summary>
    [Test]
    public void AReply_IsDashed_InItsSendersTone()
    {
        string svg = DiagramRenderers.Render(Chat());

        Assert.Multiple(() =>
        {
            Assert.That(svg, Does.Contain("class=\"edge dashed tone-green\""));
            Assert.That(svg, Does.Contain("class=\"edge tone-blue\""));
        });
    }

    /// <summary>
    /// A message's words stand in a chip in its sender's tone, as a flow's arrow words do, so a lifeline it crosses
    /// runs behind it rather than through the words.
    /// </summary>
    [Test]
    public void AMessagesWords_StandInAChip()
    {
        // Unnumbered, so the only toned chips are the words'.
        var chat = Chat();
        string svg = DiagramRenderers.Render(chat with { Sequence = chat.Sequence! with { Numbered = false } });

        Assert.That(svg, Does.Contain("class=\"word-chip tone-violet\""));
    }

    /// <summary>
    /// A frame's panel lies under the lifelines, but its tab stands over them, so no lifeline runs through a block's
    /// kind or label.
    /// </summary>
    [Test]
    public void AFramesTab_StandsOverTheLifelines()
    {
        string svg = DiagramRenderers.Render(Chat());

        Assert.That(svg.IndexOf(">loop<", StringComparison.Ordinal),
            Is.GreaterThan(svg.IndexOf("class=\"edge dashed\"", StringComparison.Ordinal)));
    }

    /// <summary>Framed for the docs, a sequence's legend names the tones its participants wear.</summary>
    [Test]
    public void TheDocsFrame_HasALegendOfTheParticipantsTones()
    {
        string svg = DiagramRenderers.RenderTitled(Chat());

        Assert.That(svg, Does.Contain("starts it").And.Contain("outside the process").And.Contain("the framework"));
    }
}
