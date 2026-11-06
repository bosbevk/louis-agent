using LouisAgent.Web.Chats;

namespace LouisAgent.Web.Tests.Chats;

/// <summary>
/// A specialist's sub-turn split for its card: its answer is the text after its last thinking or tool call, and the
/// notes it wrote between tool calls belong to its work.
/// </summary>
public class SpecialistAnswerTests
{
    /// <summary>A thinking block.</summary>
    private static Block Thinking() => new() { Kind = BlockKind.Thinking, Text = "hmm" };

    /// <summary>A finished tool call.</summary>
    private static Block Tool(string name) => new() { Kind = BlockKind.Tool, ToolName = name, ToolResult = "ok" };

    /// <summary>A text block.</summary>
    private static Block Text(string text) => new() { Kind = BlockKind.Text, Text = text };

    /// <summary>The answer is the text after the last call; notes between calls are work.</summary>
    [Test]
    public void TheAnswer_IsTheTextAfterTheLastCall_TheNotesBetweenCallsAreWork()
    {
        var sub = new Turn { Blocks = [Thinking(), Tool("WebSearch"), Text("Gevind, nou lees ek."), Tool("FetchUrl"), Text("Die antwoord.")] };

        var (work, answer) = SpecialistAnswer.Split(sub);

        Assert.That(work, Has.Count.EqualTo(4));
        Assert.That(work[2].Text, Is.EqualTo("Gevind, nou lees ek."));
        Assert.That(answer, Is.EqualTo("Die antwoord."));
    }

    /// <summary>Only text is all answer, as paragraphs, with empty pieces skipped.</summary>
    [Test]
    public void OnlyText_IsAllAnswer_JoinedAsParagraphs_EmptyPiecesSkipped()
    {
        var sub = new Turn { Blocks = [Text("Een."), Text(""), Text("Twee.")] };

        var (work, answer) = SpecialistAnswer.Split(sub);

        Assert.That(work, Is.Empty);
        Assert.That(answer, Is.EqualTo("Een.\n\nTwee."));
    }

    /// <summary>No text after the last call means no answer yet.</summary>
    [Test]
    public void NoTextAfterTheLastCall_IsNoAnswerYet()
    {
        var sub = new Turn { Blocks = [Text("Ek soek."), Tool("WebSearch")] };

        var (work, answer) = SpecialistAnswer.Split(sub);

        Assert.That(work, Has.Count.EqualTo(2));
        Assert.That(answer, Is.Empty);
    }
}
