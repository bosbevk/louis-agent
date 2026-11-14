using LouisAgent.Diagrams.Engine.Svg;
using LouisAgent.Diagrams.Engine.Svg.Renderers.Parts.Lifelines;
using LouisAgent.Diagrams.Icons;
using LouisAgent.Diagrams.Sequence.Spec;
using LouisAgent.Diagrams.Sequence.Spec.Steps;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Tests.Svg.Renderers.Parts.Lifelines;

/// <summary>
/// A sequence is drawn in the cards' look (F134.10): the docs' tool and the chat both draw one through the kind's
/// one renderer, so both get the badges, the beams and the key.
/// </summary>
public class SequenceRendererTests
{
    /// <summary>A person asks the API, which answers.</summary>
    private static readonly DiagramSource Chat = new(DiagramKind.Sequence, "One chat turn",
        Sequence: new SequenceSpec(
            [
                new Participant("you", "You", Actor: true, Tone: DiagramTone.Blue),
                new Participant("api", "louis-agent.api", DiagramIcon.Server, DiagramTone.Green)
            ],
            [new MessageStep("you", "api", "POST /v1/sessions"), new ReplyStep("api", "you", "the session")]));

    /// <summary>
    /// The renderer hands the picture to the new drawing: badges, a beam, the picture's own title and key, and
    /// none of the old boxes and chips.
    /// </summary>
    [Test]
    public void ASequence_IsDrawnInTheNewLook()
    {
        string drawn = DiagramRenderers.Render(Chat);

        Assert.Multiple(() =>
        {
            Assert.That(drawn, Is.EqualTo(SequenceDrawing.Of(Chat)));
            Assert.That(drawn, Does.Contain("class=\"mrq-title\"").And.Contain("seq-badge tone-blue").And
                .Contain("class=\"seq-beam\"").And.Contain("class=\"seq-key\""));
            Assert.That(drawn, Does.Not.Contain("class=\"box").And.Not.Contain("class=\"word-chip"));
        });
    }

    /// <summary>
    /// The chat's copy is the same drawing, kept to itself, and it keeps the look's paints and the arrowheads its
    /// lines wear.
    /// </summary>
    [Test]
    public void TheChatsCopy_IsTheSameDrawing_WithItsPaintsAndArrowheads()
    {
        string page = DiagramRenderers.RenderForPage(Chat);

        Assert.That(page, Does.Contain("seq-badge tone-blue").And.Contain("<linearGradient id=\"seq-beam\"").And
            .Contain("<marker id=\"arrow-blue\"").And.Contain("@scope"));
    }
}
