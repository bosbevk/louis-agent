using LouisAgent.Diagrams.Classes.Spec;
using LouisAgent.Diagrams.Sources;
using LouisAgent.Diagrams.Svg;
using LouisAgent.Diagrams.Svg.Renderers.Parts.Types;

namespace LouisAgent.Diagrams.Tests.Svg.Renderers;

/// <summary>
/// A class chart is drawn in the cards' look (F134.11): the docs' tool and the chat both draw one through the
/// kind's one renderer, so both get the cards, the lines that say their kind, and the key.
/// </summary>
public class ClassesRendererTests
{
    /// <summary>A host that holds a provider behind its interface.</summary>
    private static readonly DiagramSource Speech = new(DiagramKind.Classes, "Speech",
        Classes: new ClassesSpec(
            [
                new ClassType("host", "SpeechHost"), new ClassType("contract", "ISpeechProvider", "interface"),
                new ClassType("whisper", "WhisperSpeechProvider")
            ],
            [
                new ClassRelation("host", "contract", RelationKind.Holds),
                new ClassRelation("whisper", "contract", RelationKind.Implements)
            ]));

    /// <summary>
    /// The renderer hands the picture to the new drawing: glass cards with their badges, a line a kind, the
    /// picture's own title and key, and none of the old boxes and chips.
    /// </summary>
    [Test]
    public void AClassChart_IsDrawnInTheNewLook()
    {
        string drawn = DiagramRenderers.Render(Speech);

        Assert.Multiple(() =>
        {
            Assert.That(drawn, Is.EqualTo(ClassesDrawing.Of(Speech)));
            Assert.That(drawn, Does.Contain("class=\"mrq-title\"").And.Contain("class=\"cls-line holds\"").And
                .Contain("class=\"cls-mark\"").And.Contain("class=\"cls-key\""));
            Assert.That(drawn, Does.Not.Contain("class=\"box").And.Not.Contain("class=\"word-chip"));
        });
    }

    /// <summary>
    /// The chat's copy is the same drawing, kept to itself, and it keeps the look's paints and the ends its lines
    /// wear.
    /// </summary>
    [Test]
    public void TheChatsCopy_IsTheSameDrawing_WithItsPaintsAndEnds()
    {
        string page = DiagramRenderers.RenderForPage(Speech);

        Assert.That(page, Does.Contain("class=\"cls-line holds\"").And.Contain("<linearGradient id=\"mrq-glass\"").And
            .Contain("<marker id=\"diamond-violet\"").And.Contain("<marker id=\"triangle-violet\"").And
            .Contain("@scope"));
    }
}
