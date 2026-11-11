using LouisAgent.Web.Chats;
using static LouisAgent.Web.Tests.StreamEvents;
using LouisAgent.SessionStream;
using LouisAgent.Web.Api;

namespace LouisAgent.Web.Tests.Chats;

/// <summary>
/// The images a tool gave. A tool_result whose text holds a markdown image gives its block that URL when the result
/// arrives, before anything can cut the text; a sub-session's result does the same on its own turn; a plain link is no
/// image, a failed result gives none, and neither does the result of a delegation call (that text is the specialist's
/// own answer, written by a model, not something a tool read).
/// </summary>
public class TurnStreamImageTests
{
    private const string Page = "# Bobotie\n\n![Bobotie](https://example.co.za/bobotie.jpg)\n\nIngredients";

    /// <summary>The turn these events fold into.</summary>
    private static Turn Apply(params SessionEvent[] events)
    {
        var turn = new Turn();
        foreach (var streamEvent in events) TurnStream.Apply(turn, streamEvent);
        return turn;
    }

    /// <summary>A tool result with an image gives its block the address.</summary>
    [Test]
    public void AToolResultWithAnImage_GivesItsBlockTheUrl()
    {
        var turn = Apply(ToolUse("c1", "FetchUrl"), ToolResult("c1", Page));

        Assert.That(turn.Blocks[0].Images, Is.EqualTo(new[] { "https://example.co.za/bobotie.jpg" }));
    }

    /// <summary>A sub-session's result gives the address to its own turn's block.</summary>
    [Test]
    public void ASubSessionsToolResult_GivesTheUrlToItsOwnTurnsBlock()
    {
        var turn = Apply(
            ToolUse("d1", "AskAgent"),
            InSubSession("d1", ToolUse("c1", "FetchUrl")),
            InSubSession("d1", ToolResult("c1", Page)));

        Assert.That(turn.Blocks[0].SubTurn!.Blocks[0].Images,
            Is.EqualTo(new[] { "https://example.co.za/bobotie.jpg" }));
        Assert.That(turn.Blocks[0].Images, Is.Empty, "the parent's own call has none");
    }

    /// <summary>A plain link is no image.</summary>
    [Test]
    public void APlainLink_IsNoImage() =>
        Assert.That(
            Apply(ToolUse("c1", "FetchUrl"), ToolResult("c1", "See [the page](https://example.co.za/bobotie)"))
                .Blocks[0].Images, Is.Empty);

    /// <summary>A failed result gives none.</summary>
    [Test]
    public void AFailedResult_GivesNone() =>
        Assert.That(Apply(ToolUse("c1", "FetchUrl"), ToolResult("c1", Page, failed: true)).Blocks[0].Images, Is.Empty);

    /// <summary>A delegation call's result gives none, since it's the specialist's answer.</summary>
    [Test]
    public void TheResultOfADelegationCall_GivesNone_BecauseItIsTheSpecialistsAnswerNotWhatAToolRead() =>
        Assert.That(
            Apply(ToolUse("d1", "AskAgent"), ToolResult("d1", "![Bobotie](https://evil.example/?q=the+chat)"))
                .Blocks[0].Images, Is.Empty);
}
