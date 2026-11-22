using LouisAgent.Core.Usage.Records;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Transcripts;
using Microsoft.Extensions.AI;

namespace LouisAgent.Evals.Tests.Grading;

/// <summary>
/// The judge marks one criterion with one model call (F126.2 step 10), on a fake model: what the model is told is
/// pinned to a golden file, the answer is asked for as a typed record in the ledger's grading scope, and a judge that
/// can't mark says why rather than answering "not met".
/// </summary>
public class JudgeTests
{
    /// <summary>The golden file that holds what the judge's model is told.</summary>
    private const string RequestFile = "src/tests/evals/LouisAgent.Evals.Tests/Grading/Golden/JudgeRequest.txt";

    /// <summary>The criterion every test marks.</summary>
    private const string Criterion = "Names the doc it read";

    /// <summary>Two turns of the guide's, the second with a search and a read, one of which failed.</summary>
    private static readonly IReadOnlyList<TurnTranscript> Conversation =
    [
        new("What is louis-agent?", "An AI agent framework in C#.", [], TimeSpan.FromSeconds(4)),
        new("Where does it say so?", "The overview says so, in docs/README.md.",
        [
            new ToolCallTranscript("SearchDocs", """{"query":"what is louis-agent"}""", "docs/README.md"),
            new ToolCallTranscript("ReadDoc", """{"path":"docs/missing.md"}""", "No such doc.", Failed: true)
        ], TimeSpan.FromSeconds(9))
    ];

    /// <summary>A model that answers with this text.</summary>
    private static FakeChatClient Answering(string text) =>
        new(_ => new ChatResponse(new ChatMessage(ChatRole.Assistant, text)));

    /// <summary>
    /// What the judge's model is told is the golden file's text: the instructions, each turn, the criterion.
    /// </summary>
    [Test]
    public async Task WhatTheModelIsTold_IsTheGoldenFile()
    {
        var model = Answering("""{"reason":"It names docs/README.md.","met":true}""");

        await new Judge(model).MarkAsync(Criterion, Conversation);

        string told = string.Join("\n\n", model.Requests[0].Messages.Select(m => $"[{m.Role}]\n{m.Text}")) + "\n";
        Golden.Match(RequestFile, told);
    }

    /// <summary>
    /// The request asks for a JSON schema and sets no temperature, since the newest models refuse one.
    /// </summary>
    [Test]
    public async Task TheRequest_AsksForASchema_AndSetsNoTemperature()
    {
        var model = Answering("""{"reason":"It names it.","met":true}""");

        await new Judge(model).MarkAsync(Criterion, Conversation);

        var options = model.Requests[0].Options;
        Assert.Multiple(() =>
        {
            Assert.That(options?.ResponseFormat, Is.InstanceOf<ChatResponseFormatJson>());
            Assert.That(((ChatResponseFormatJson)options!.ResponseFormat!).Schema, Is.Not.Null);
            Assert.That(options.Temperature, Is.Null);
        });
    }

    /// <summary>The call runs in the ledger's grading scope, so its cost lands under its own purpose.</summary>
    [Test]
    public async Task TheCall_RunsInTheGradingScope()
    {
        string? purpose = null;
        var model = new FakeChatClient(_ =>
        {
            purpose = UsageScope.Current?.Purpose;
            return new ChatResponse(new ChatMessage(ChatRole.Assistant, """{"reason":"Yes.","met":true}"""));
        });

        await new Judge(model).MarkAsync(Criterion, Conversation);

        Assert.That(purpose, Is.EqualTo(UsagePurpose.Grading));
    }

    /// <summary>
    /// The model's answer comes back as a mark with the criterion's words, met or not, and the reason.
    /// </summary>
    [TestCase(true)]
    [TestCase(false)]
    public async Task TheAnswer_ComesBackAsAMark(bool met)
    {
        var model = Answering($$"""{"reason":"It names docs/README.md.","met":{{(met ? "true" : "false")}}}""");

        var mark = await new Judge(model).MarkAsync(Criterion, Conversation);

        Assert.That(mark, Is.EqualTo(new Mark(Criterion, met, "It names docs/README.md.")));
    }

    /// <summary>An empty answer, and one that doesn't fit, each throw with the reason.</summary>
    [TestCase("", "no answer")]
    [TestCase("Yes, it does.", "doesn't fit")]
    [TestCase("""{"reason":"","met":true}""", "doesn't fit")]
    public void AnAnswerThatCantBeAMark_Throws(string text, string reason) =>
        Assert.That(() => new Judge(Answering(text)).MarkAsync(Criterion, Conversation),
            Throws.InstanceOf<JudgeFailedException>().With.Message.Contains(reason));

    /// <summary>A failure from the provider throws with the reason, never as "not met".</summary>
    [Test]
    public void AFailureFromTheProvider_Throws()
    {
        var model = new FakeChatClient(_ => throw new HttpRequestException("503 Service Unavailable"));

        Assert.That(() => new Judge(model).MarkAsync(Criterion, Conversation),
            Throws.InstanceOf<JudgeFailedException>().With.Message.Contains("503"));
    }

    /// <summary>A cancelled call stays a cancellation.</summary>
    [Test]
    public void ACancelledCall_StaysACancellation()
    {
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        var model = new FakeChatClient(_ => throw new OperationCanceledException(cancelled.Token));

        Assert.That(() => new Judge(model).MarkAsync(Criterion, Conversation, cancelled.Token),
            Throws.InstanceOf<OperationCanceledException>());
    }
}
