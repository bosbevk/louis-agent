using LouisAgent.Core.Web;
using LouisAgent.Composition.Tools;
using LouisAgent.Choices;
using LouisAgent.Core.Agent.Turns;
using LouisAgent.Core.Agent.Toolsets;
using System.Text.Json;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Agent.Turns.Guards;
using LouisAgent.Core.Config;
using LouisAgent.Core.Prompting;
using LouisAgent.Core.Prompting.Providers;
using Microsoft.Extensions.AI;
using LouisAgent.Core.Web.Search;

namespace LouisAgent.Core.Tests.Agent;

/// <summary>
/// The engine end to end on a fake model: which tools it offers, the tool-call loop, skills run as data, oversized and
/// cut-off results, streaming, and thinking.
/// </summary>
public class AgentEngineTests
{
    /// <summary>The sample skills, SayHello and GetCurrentTime among them.</summary>
    private static ISkillProvider HelloSkills() =>
        new MarkdownSkillProvider(TestPaths.SampleSkills);

    /// <summary>An engine with the files toolset and the self-extension tools, on the model given.</summary>
    private static AgentEngine Engine(IChatClient client, bool supportsTools = true)
    {
        var options = TestPaths.Agent();
        string[] toolsets = [TestToolset.Files.Wire()];
        return new(HelloSkills(), client, options, supportsTools, toolsets: TestPaths.Toolsets(options, toolsets),
            selfExtension: true);
    }

    /// <summary>
    /// The engine offers the workspace's tools and the clock, and nothing for a service it has no key for.
    /// </summary>
    [Test]
    public void Constructor_ExposesWorkspaceToolsAndTheClock()
    {
        var engine = Engine(new FakeChatClient());
        var names = engine.Tools.Select(t => t.Name).ToList();

        Assert.That(names, Does.Contain("GetToday"));
        Assert.That(names, Does.Contain("ReadWorkspaceFile"));
        Assert.That(names, Does.Not.Contain("LogTimeByTaskName"), "no Paymo key configured");
    }

    /// <summary>
    /// With every toolset loaded, no two tools share a name, since the Anthropic API rejects a request where they do.
    /// </summary>
    [Test]
    public void Constructor_RegistersUniqueToolNamesAcrossEveryToolset()
    {
        var options = new AgentOptions
        {
            WorkspaceRoot = Path.GetTempPath(),
        };
        var context = new ToolsetContext(options, WebAccess.Create(options));
        var toolsets = ToolsetCatalog.BuildTools(TestTools.Catalog.Toolsets, context);

        var engine = new AgentEngine(new CompositeSkillProvider(), new FakeChatClient(), options,
            toolsets: toolsets, selfExtension: true);

        var duplicates = engine.Tools.GroupBy(t => t.Name).Where(g => g.Count() > 1).Select(g => g.Key);
        Assert.That(duplicates, Is.Empty);
        Assert.That(engine.Tools.Select(t => t.Name), Does.Contain("DotNetBuild"));
    }

    /// <summary>An engine given its own tool objects offers only those and the clock.</summary>
    [Test]
    public void Constructor_WithToolsets_RegistersOnlyThoseAndTheClock()
    {
        Assert.That(PingEngine(new FakeChatClient()).Tools.Select(t => t.Name),
            Is.EquivalentTo(new[] { "GetToday", "Ping" }));
    }

    /// <summary>An engine whose one tool of its own is <see cref="PingTools"/>.</summary>
    private static AgentEngine PingEngine(IChatClient client) =>
        new(HelloSkills(), client, TestPaths.Agent(), toolsets: [new PingTools()]);

    /// <summary>
    /// An engine whose toolsets are all safe to run together runs a round's calls at once; any other keeps them in
    /// order, as a file written and then the tests run must be.
    /// </summary>
    [Test]
    public async Task AParallelEngine_RunsARoundsCallsAtOnce_AnyOtherOneAfterAnother()
    {
        // How many of a round's three calls ran at the same time.
        async Task<int> MostAtOnce(bool concurrent)
        {
            // In parallel, each call is held until all three have started, so a busy machine can't let one finish
            // before the last begins (FX220); in order, each just takes a moment.
            var tools = new SlowTools(concurrent ? TimeSpan.FromSeconds(5) : TimeSpan.FromMilliseconds(150));
            var client = new FakeChatClient(
                _ => new ChatResponse(new ChatMessage(ChatRole.Assistant,
                [
                    new FunctionCallContent("c1", "Slow"), new FunctionCallContent("c2", "Slow"),
                    new FunctionCallContent("c3", "Slow")
                ])),
                _ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "done")));
            var engine = new AgentEngine(HelloSkills(), client, TestPaths.Agent(), toolsets: [tools],
                concurrentToolCalls: concurrent);
            await engine.ProcessPromptAsync(engine.StartConversation(), "go");
            return tools.MostAtOnce;
        }

        Assert.That(await MostAtOnce(concurrent: true), Is.EqualTo(3));
        Assert.That(await MostAtOnce(concurrent: false), Is.EqualTo(1));
    }

    /// <summary>
    /// A tool that counts how many of its calls run at once. Each call waits until a round's three calls have all
    /// started, or until its hold runs out, so a parallel round is counted whole however slow the machine is.
    /// </summary>
    private sealed class SlowTools(TimeSpan hold)
    {
        /// <summary>The calls a round makes, which the gate waits for.</summary>
        private const int RoundsCalls = 3;

        private readonly TaskCompletionSource _allStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _running;
        public int MostAtOnce;

        /// <summary>Counts itself in, waits for the round or its hold, then counts itself out.</summary>
        [System.ComponentModel.Description("Takes a moment")]
        public async Task<string> Slow()
        {
            int now = Interlocked.Increment(ref _running);
            lock (this) MostAtOnce = Math.Max(MostAtOnce, now);

            // The last of the round to start opens the gate for all of them.
            if (now == RoundsCalls)
                _allStarted.TrySetResult();
            await Task.WhenAny(_allStarted.Task, Task.Delay(hold));

            Interlocked.Decrement(ref _running);
            return "done";
        }
    }

    /// <summary>A tool object with one tool, for an engine given its own toolsets.</summary>
    private sealed class PingTools
    {
        /// <summary>Answers pong.</summary>
        [System.ComponentModel.Description("Replies pong")]
        public string Ping() => "pong";
    }

    /// <summary>A prompt runs the tool the model calls, gives it the result, and returns the final answer.</summary>
    [Test]
    public async Task ProcessPrompt_RunsToolCallLoopAndReturnsFinalAnswer()
    {
        var client = new FakeChatClient(
            _ => new ChatResponse(new ChatMessage(ChatRole.Assistant,
            [
                new FunctionCallContent("call_1", "Ping", new Dictionary<string, object?>())
            ])),
            messages =>
            {
                var toolResult = messages.Last().Contents.OfType<FunctionResultContent>().Single();
                return new ChatResponse(new ChatMessage(ChatRole.Assistant, $"Ping said: {toolResult.Result}"));
            });
        var engine = PingEngine(client);
        var history = engine.StartConversation();

        string answer = await engine.ProcessPromptAsync(history, "ping it");

        Assert.That(answer, Does.Contain("Ping said: pong"));
        Assert.That(client.Requests, Has.Count.EqualTo(2));
        Assert.That(client.Requests[0].Options!.Tools!.Select(t => t.Name), Does.Contain("Ping"));
        Assert.That(history.Messages.Select(m => m.Role), Does.Contain(ChatRole.Tool),
            "tool result is kept in history");
    }

    /// <summary>A model without tools is sent none, and is told so in the chat.</summary>
    [Test]
    public async Task ProcessPrompt_WithoutToolSupport_SendsNoToolsAndAddsNotice()
    {
        var client = new FakeChatClient(_ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "I can't do that")));
        var engine = Engine(client, supportsTools: false);

        await engine.ProcessPromptAsync(engine.StartConversation(), "log time please");

        Assert.That(client.Requests[0].Options!.Tools, Is.Null.Or.Empty);
        Assert.That(client.Requests[0].Messages.Select(m => m.Text), Does.Contain(TurnRunner.NoToolsNotice));
    }

    /// <summary>
    /// A tool result too big for the chat is summarised by a request without tools, and only the summary goes on.
    /// </summary>
    [Test]
    public async Task ProcessPrompt_OversizedToolResult_IsSummarisedBeforeReachingModel()
    {
        string relative = $"big-{Guid.NewGuid():N}.txt";
        string path = Path.Combine(TestPaths.RepoRoot, relative);
        File.WriteAllText(path, new string('x', OversizedResults.MaxToolResultChars + 1));
        try
        {
            var client = new FakeChatClient(
                _ => new ChatResponse(new ChatMessage(ChatRole.Assistant,
                [
                    new FunctionCallContent("call_1", "ReadWorkspaceFile",
                        new Dictionary<string, object?> { ["relativePath"] = relative })
                ])),
                _ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "a file full of x")),
                messages =>
                {
                    var toolResult = messages.Last().Contents.OfType<FunctionResultContent>().Single();
                    return new ChatResponse(new ChatMessage(ChatRole.Assistant, toolResult.Result!.ToString()!));
                });
            var engine = Engine(client);

            string answer = await engine.ProcessPromptAsync(engine.StartConversation(), "what is in the file?");

            Assert.That(answer, Does.Contain("Summary:").And.Contain("a file full of x"));
            Assert.That(answer.Length, Is.LessThan(1_000), "raw output never reaches the main conversation");
            Assert.That(client.Requests[1].Options!.Tools, Is.Null, "summariser call has no tools");
            Assert.That(client.Requests[1].Messages.Last().Text, Does.Contain("what is in the file?"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>When the summary request fails, the result is cut short instead, and says so.</summary>
    [Test]
    public async Task SummariseToolResult_WhenModelFails_FallsBackToTruncation()
    {
        var client = new FakeChatClient(_ => throw new HttpRequestException("boom"));
        var engine = Engine(client);
        string output = new('y', OversizedResults.MaxToolResultChars * 3);

        string result = await engine.Guards.Oversized.SummariseAsync("DiffUnstaged", "{}", output, "show my changes");

        Assert.That(result, Does.Contain("too large").And.Contain("[truncated"));
        Assert.That(result.Length, Is.LessThan(OversizedResults.MaxToolResultChars + 500));
    }

    /// <summary>A tool call cut off by the output limit never runs, and the model is told to write in chunks.</summary>
    [Test]
    public async Task ProcessPrompt_ToolCallCutOffByOutputLimit_IsNotRunAndModelIsToldToChunk()
    {
        string relative = $"cut-off-{Guid.NewGuid():N}.html";
        var client = new FakeChatClient(
            _ => new ChatResponse(new ChatMessage(ChatRole.Assistant,
            [
                new FunctionCallContent("call_1", "WriteWorkspaceFile",
                    new Dictionary<string, object?> { ["relativePath"] = relative })
            ]))
            { FinishReason = ChatFinishReason.Length },
            messages =>
            {
                var toolResult = messages.Last().Contents.OfType<FunctionResultContent>().Single();
                return new ChatResponse(new ChatMessage(ChatRole.Assistant, toolResult.Result!.ToString()!));
            });
        var engine = Engine(client);

        string answer = await engine.ProcessPromptAsync(engine.StartConversation(), "make me a web page");

        Assert.That(answer, Is.EqualTo(ToolCallGuards.TruncatedCallMessage));
        Assert.That(File.Exists(Path.Combine(TestPaths.RepoRoot, relative)), Is.False, "the partial call never ran");
    }

    /// <summary>A call missing an argument tells the model which one.</summary>
    [Test]
    public async Task ProcessPrompt_ToolCallMissingArgument_TellsModelWhichOne()
    {
        var client = new FakeChatClient(
            _ => new ChatResponse(new ChatMessage(ChatRole.Assistant,
            [
                new FunctionCallContent("call_1", "WriteWorkspaceFile",
                    new Dictionary<string, object?> { ["relativePath"] = "never-written.txt" })
            ])),
            messages =>
            {
                var toolResult = messages.Last().Contents.OfType<FunctionResultContent>().Single();
                return new ChatResponse(new ChatMessage(ChatRole.Assistant, toolResult.Result!.ToString()!));
            });
        var engine = Engine(client);

        string answer = await engine.ProcessPromptAsync(engine.StartConversation(), "write a file");

        Assert.That(answer, Does.Contain("content"), "detailed error names the missing parameter");
    }

    /// <summary>
    /// A streamed turn yields text, the call and its result as they happen, and keeps them all after.
    /// </summary>
    [Test]
    public async Task StreamPrompt_YieldsTextToolCallAndResultAsTheyHappen_AndRecordsHistory()
    {
        var client = new FakeChatClient(
            _ => new ChatResponse(new ChatMessage(ChatRole.Assistant,
            [
                new TextContent("Let me run it. "),
                new FunctionCallContent("call_1", "Ping", new Dictionary<string, object?>())
            ])),
            _ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "All done.")));
        var engine = PingEngine(client);
        var history = engine.StartConversation();

        var contents = new List<AIContent>();
        await foreach (var update in engine.StreamPromptAsync(history, "ping it"))
            contents.AddRange(update.Contents);

        Assert.That(contents.OfType<TextContent>().Select(t => t.Text),
            Is.EqualTo(new[] { "Let me run it. ", "All done." }));
        Assert.That(contents.OfType<FunctionCallContent>().Single().Name, Is.EqualTo("Ping"));
        Assert.That(contents.OfType<FunctionResultContent>().Single().Result?.ToString(), Does.Contain("pong"));
        Assert.That(history.Messages.Last().Text, Is.EqualTo("All done."));
        Assert.That(history.Messages.Select(m => m.Role), Does.Contain(ChatRole.Tool),
            "tool result is kept in history");
    }

    /// <summary>Streamed too, a tool call cut off by the output limit never runs.</summary>
    [Test]
    public async Task StreamPrompt_ToolCallCutOffByOutputLimit_IsNotRun()
    {
        string relative = $"cut-off-{Guid.NewGuid():N}.html";
        var client = new FakeChatClient(
            _ => new ChatResponse(new ChatMessage(ChatRole.Assistant,
            [
                new FunctionCallContent("call_1", "WriteWorkspaceFile",
                    new Dictionary<string, object?> { ["relativePath"] = relative })
            ]))
            { FinishReason = ChatFinishReason.Length },
            _ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok")));
        var engine = Engine(client);

        var results = new List<FunctionResultContent>();
        await foreach (var update in engine.StreamPromptAsync(engine.StartConversation(), "make me a web page"))
            results.AddRange(update.Contents.OfType<FunctionResultContent>());

        Assert.That(results.Single().Result, Is.EqualTo(ToolCallGuards.TruncatedCallMessage));
        Assert.That(File.Exists(Path.Combine(TestPaths.RepoRoot, relative)), Is.False, "the partial call never ran");
    }

    /// <summary>A streamed reply cut off by the output limit is continued with one more request.</summary>
    [Test]
    public async Task StreamPrompt_ReplyCutOffByOutputLimit_AsksModelToContinue()
    {
        // A streamed tool call cut off mid-arguments is dropped, leaving only the text before it.
        var client = new FakeChatClient(
            _ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "Now I'll append the rest:"))
            {
                FinishReason = ChatFinishReason.Length
            },
            _ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "Done in smaller chunks.")));
        var engine = Engine(client);
        var history = engine.StartConversation();

        var text = new List<string>();
        await foreach (var update in engine.StreamPromptAsync(history, "write a big file"))
            text.AddRange(update.Contents.OfType<TextContent>().Select(t => t.Text));

        Assert.That(text, Is.EqualTo(new[] { "Now I'll append the rest:", "Done in smaller chunks." }));
        Assert.That(client.Requests[1].Messages.Last().Text, Is.EqualTo(TurnRunner.ContinueAfterCutOffMessage));
        Assert.That(history.Messages.Last().Text, Is.EqualTo("Done in smaller chunks."));
    }

    /// <summary>With thinking on, the request asks for it and the thinking streams out.</summary>
    [Test]
    public async Task StreamPrompt_WithThinking_RequestsReasoningAndStreamsIt()
    {
        var client = new FakeChatClient(_ => new ChatResponse(new ChatMessage(ChatRole.Assistant,
            [new TextReasoningContent("Considering the request."), new TextContent("Answer.")])));
        var engine = new AgentEngine(HelloSkills(), client, TestPaths.Agent()) { Thinking = ReasoningEffort.Medium };

        var contents = new List<AIContent>();
        await foreach (var update in engine.StreamPromptAsync(engine.StartConversation(), "think about it"))
            contents.AddRange(update.Contents);

        Assert.That(client.Requests[0].Options!.Reasoning!.Effort, Is.EqualTo(ReasoningEffort.Medium));
        Assert.That(contents.OfType<TextReasoningContent>().Single().Text, Is.EqualTo("Considering the request."));
    }

    /// <summary>With thinking off, the request asks for none.</summary>
    [Test]
    public async Task StreamPrompt_WithoutThinking_SendsNoReasoning()
    {
        var client = new FakeChatClient(_ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "Answer.")));
        var engine = Engine(client);

        await foreach (var _ in engine.StreamPromptAsync(engine.StartConversation(), "hi"))
        {
        }

        Assert.That(client.Requests[0].Options!.Reasoning, Is.Null);
    }
}
