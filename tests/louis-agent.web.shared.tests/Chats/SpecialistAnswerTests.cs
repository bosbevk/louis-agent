using LouisAgent.Web.Shared.Chats;

namespace LouisAgent.Web.Shared.Tests.Chats;

// W2b: a specialist's sub-turn split for its card: its answer is the text after its last thinking or tool call, and
// the notes it wrote between tool calls belong to its work.
public class SpecialistAnswerTests
{
    private static Block Thinking() => new() { Kind = BlockKind.Thinking, Text = "hmm" };
    private static Block Tool(string name) => new() { Kind = BlockKind.Tool, ToolName = name, ToolResult = "ok" };
    private static Block Text(string text) => new() { Kind = BlockKind.Text, Text = text };

    [Test]
    public void TheAnswer_IsTheTextAfterTheLastCall_TheNotesBetweenCallsAreWork()
    {
        var sub = new Turn { Blocks = [Thinking(), Tool("WebSearch"), Text("Gevind, nou lees ek."), Tool("FetchUrl"), Text("Die antwoord.")] };

        var (work, answer) = SpecialistAnswer.Split(sub);

        Assert.That(work, Has.Count.EqualTo(4));
        Assert.That(work[2].Text, Is.EqualTo("Gevind, nou lees ek."));
        Assert.That(answer, Is.EqualTo("Die antwoord."));
    }

    [Test]
    public void OnlyText_IsAllAnswer_JoinedAsParagraphs_EmptyPiecesSkipped()
    {
        var sub = new Turn { Blocks = [Text("Een."), Text(""), Text("Twee.")] };

        var (work, answer) = SpecialistAnswer.Split(sub);

        Assert.That(work, Is.Empty);
        Assert.That(answer, Is.EqualTo("Een.\n\nTwee."));
    }

    [Test]
    public void NoTextAfterTheLastCall_IsNoAnswerYet()
    {
        var sub = new Turn { Blocks = [Text("Ek soek."), Tool("WebSearch")] };

        var (work, answer) = SpecialistAnswer.Split(sub);

        Assert.That(work, Has.Count.EqualTo(2));
        Assert.That(answer, Is.Empty);
    }
}
