using LouisAgent.Choices;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Agent.Turns;
using LouisAgent.Core.Llm.UsageMapping;
using LouisAgent.Core.Usage.Records;
using LouisAgent.Core.Usage.Sinks;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Providers;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Agent.Turns.Guards;
using Microsoft.Extensions.AI;
using LouisAgent.Core.Anthropic;

namespace LouisAgent.Core.Tests.Agent;

/// <summary>
/// Every request the engine makes is recorded: each round of the tool loop, a summary and a continuation. Hosts open
/// the turn's usage scope, and these tests do it the same way.
/// </summary>
public class AgentEngineUsageTests
{
    /// <summary>An engine on the model given, recording into the sink given, with the mapper if one is given.</summary>
    private static AgentEngine Engine(IChatClient client, IUsageSink sink, IUsageMapper? mapper = null) =>
        new(new MarkdownSkillProvider(TestPaths.SampleSkills), client,
            TestPaths.Agent(),
            toolsets: TestPaths.Toolsets(TestPaths.Agent(), [TestToolset.Files.Wire()]), usageSink: sink,
            usageMapper: mapper);

    /// <summary>The model's answer that runs the SayHello skill, under the call id given.</summary>
    private static ChatResponse CallSayHello(string callId) =>
        new(new ChatMessage(ChatRole.Assistant,
        [
            new FunctionCallContent(callId, "ExecuteSkill",
                new Dictionary<string, object?>
                {
                    ["skillName"] = "SayHello",
                    ["jsonArgs"] = "{\"name\":\"Agent\"}"
                })
        ]));

    /// <summary>A streamed turn of three rounds records three requests, numbered 1 to 3.</summary>
    [Test]
    public async Task StreamPrompt_ThreeRoundToolLoop_RecordsRounds1To3()
    {
        var sink = new ListUsageSink();
        var client = new FakeChatClient(
            _ => CallSayHello("call_1"),
            _ => CallSayHello("call_2"),
            _ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "Said hello twice.")));
        var engine = Engine(client, sink);

        using (UsageScope.Begin())
            await foreach (var _ in engine.StreamPromptAsync(engine.StartConversation(), "say hello twice"))
            {
            }

        Assert.That(sink.Records.Select(r => r.Round), Is.EqualTo(new[] { 1, 2, 3 }));
        Assert.That(sink.Records.Select(r => r.Purpose), Is.All.EqualTo(UsagePurpose.Turn));
    }

    /// <summary>A turn answered whole records its rounds too.</summary>
    [Test]
    public async Task ProcessPrompt_NonStreamingToolLoop_IsRecordedToo()
    {
        var sink = new ListUsageSink();
        var client = new FakeChatClient(
            _ => CallSayHello("call_1"),
            _ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "Done.")));
        var engine = Engine(client, sink);

        using (UsageScope.Begin())
            await engine.ProcessPromptAsync(engine.StartConversation(), "say hello");

        Assert.That(sink.Records.Select(r => r.Round), Is.EqualTo(new[] { 1, 2 }));
    }

    /// <summary>Summarising an oversized result adds one record, marked as a summary.</summary>
    [Test]
    public async Task ProcessPrompt_OversizedToolResult_AddsOneSummaryRecord()
    {
        string relative = $"big-{Guid.NewGuid():N}.txt";
        string path = Path.Combine(TestPaths.RepoRoot, relative);
        File.WriteAllText(path, new string('x', OversizedResults.MaxToolResultChars + 1));
        try
        {
            var sink = new ListUsageSink();
            var client = new FakeChatClient(
                _ => new ChatResponse(new ChatMessage(ChatRole.Assistant,
                [
                    new FunctionCallContent("call_1", "ReadWorkspaceFile",
                        new Dictionary<string, object?> { ["relativePath"] = relative })
                ])),
                _ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "a file full of x")),
                _ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "It's all x.")));
            var engine = Engine(client, sink);

            using (UsageScope.Begin(host: "api", session: "sess_1", turn: 4,
                       tags: new UsageTags("fix:e8b0", "run-1", "order-service")))
                await engine.ProcessPromptAsync(engine.StartConversation(), "what is in the file?");

            Assert.That(sink.Records.Select(r => (r.Purpose, r.Round)), Is.EqualTo(new[]
            {
                (UsagePurpose.Turn, 1), (UsagePurpose.Summary, 1),
                // The summary has its own scope, so the turn's next request is still round 2.
                (UsagePurpose.Turn, 2),
            }));
            Assert.That(sink.Records.Select(r => (r.Host, r.Session, r.Turn)),
                Is.All.EqualTo(("api", "sess_1", (int?)4)),
                "the summary is attributed to the turn it served");
            Assert.That(sink.Records.Select(r => (r.Task, r.Run, r.Service)),
                Is.All.EqualTo(("fix:e8b0", "run-1", "order-service")),
                "and to the fix and run it was part of");
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>The request that continues a cut-off reply is recorded as a continuation.</summary>
    [Test]
    public async Task StreamPrompt_ReplyCutOff_ContinuationIsRecordedAsContinue()
    {
        var sink = new ListUsageSink();
        var client = new FakeChatClient(
            _ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "Now I'll append the rest:"))
            {
                FinishReason = ChatFinishReason.Length
            },
            _ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "Done in smaller chunks.")));
        var engine = Engine(client, sink);

        using (UsageScope.Begin())
            await foreach (var _ in engine.StreamPromptAsync(engine.StartConversation(), "write a big file"))
            {
            }

        Assert.That(sink.Records.Select(r => (r.Purpose, r.Round, r.Stop)),
            Is.EqualTo(new[] { (UsagePurpose.Turn, 1, "length"), (UsagePurpose.Continue, 2, "stop"), }));
    }

    /// <summary>The engine reads usage with the provider's mapper it was given.</summary>
    [Test]
    public async Task ProcessPrompt_UsesTheMapperItIsGiven()
    {
        var sink = new ListUsageSink();
        var usage = new UsageDetails
        {
            InputTokenCount = 10_266,
            CachedInputTokenCount = 0,
            OutputTokenCount = 144,
            AdditionalCounts = new AdditionalPropertiesDictionary<long> { ["CacheCreationInputTokens"] = 10_227 },
        };
        var client = new FakeChatClient(_ =>
            new ChatResponse(new ChatMessage(ChatRole.Assistant, "hi")) { Usage = usage });
        var engine = Engine(client, sink, new AnthropicUsageMapper());

        await engine.ProcessPromptAsync(engine.StartConversation(), "hi");

        Assert.That(sink.Records.Single().Tokens, Is.EqualTo(new UsageTokens(39, 10_227, 0, 144, null)));
    }

    /// <summary>With no ledger the engine records nothing and answers all the same.</summary>
    [Test]
    public async Task ProcessPrompt_NoSink_RecordsNothingAndStillAnswers()
    {
        var client = new FakeChatClient(_ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "hi")));
        var engine = new AgentEngine(
            new MarkdownSkillProvider(TestPaths.SampleSkills), client,
            TestPaths.Agent());

        Assert.That(await engine.ProcessPromptAsync(engine.StartConversation(), "hi"), Is.EqualTo("hi"));
    }
}
