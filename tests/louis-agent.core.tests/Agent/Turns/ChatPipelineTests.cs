using LouisAgent.Choices;
using LouisAgent.Core.Agent.Toolsets;
using System.Diagnostics;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Agent.Turns;
using LouisAgent.Core.Skills.Providers;
using LouisAgent.Core.Usage.Records;
using LouisAgent.Core.Usage.Sinks;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Tests.Agent.Turns;

// F18 step 4d: a turn's trace. The spans are Microsoft.Extensions.AI's own, named by the OpenTelemetry GenAI conventions
// (the span name is "{gen_ai.operation.name} {target}", so the tests read the operation tag, not the name). No exporter
// is wired (F44); an ActivityListener stands in for one.
public class ChatPipelineTests
{
    private const string Operation = "gen_ai.operation.name";
    private static readonly ActivitySource TestSource = new("louis-agent.core.tests.chat-pipeline");

    private static readonly string[] ContentTags =
    [
        "gen_ai.input.messages", "gen_ai.output.messages", "gen_ai.system_instructions",
        "gen_ai.tool.call.arguments", "gen_ai.tool.call.result",
    ];

    private static AgentEngine Engine(IChatClient client, IUsageSink sink) =>
        new(new MarkdownSkillProvider(Path.Combine(TestPaths.RepoRoot, "Skills", "HelloWorld.md")), client, TestPaths.Agent(),
            toolsets: TestPaths.Toolsets(TestPaths.Agent(), [CoreToolset.Files.Wire()]), usageSink: sink);

    private static ChatResponse Read(string relative, string callId) =>
        new(new ChatMessage(ChatRole.Assistant,
        [
            new FunctionCallContent(callId, "ReadWorkspaceFile", new Dictionary<string, object?> { ["relativePath"] = relative })
        ]));

    private static ChatResponse Say(string text) => new(new ChatMessage(ChatRole.Assistant, text));

    /// <summary>Runs a turn under a root span and returns the spans of that trace only, so parallel tests don't mix.</summary>
    private static async Task<(Activity Root, List<Activity> Spans)> Trace(AgentEngine engine, string prompt, bool streamed)
    {
        var spans = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = a => { lock (spans) spans.Add(a); },
        };
        ActivitySource.AddActivityListener(listener);

        using var root = TestSource.StartActivity("test turn")!;
        using (UsageScope.Begin())
        {
            if (streamed)
                await foreach (var _ in engine.StreamPromptAsync(engine.NewHistory(), prompt)) { }
            else
                await engine.ProcessPromptAsync(engine.NewHistory(), prompt);
        }

        lock (spans) return (root, spans.Where(a => a.TraceId == root.TraceId).ToList());
    }

    private static List<Activity> WithOperation(IEnumerable<Activity> spans, string operation) =>
        spans.Where(a => a.GetTagItem(Operation) as string == operation).ToList();

    private static string Written(string content, out string relative, string prefix = "trace")
    {
        relative = $"{prefix}-{Guid.NewGuid():N}.txt";
        string path = Path.Combine(TestPaths.RepoRoot, relative);
        File.WriteAllText(path, content);
        return path;
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Turn_MakesOneChatSpanPerRequestAndOneToolSpanPerCall(bool streamed)
    {
        string path = Written("a small file", out string relative);
        try
        {
            var client = new FakeChatClient(
                _ => new ChatResponse(new ChatMessage(ChatRole.Assistant,
                [
                    ..Read(relative, "call_1").Messages[0].Contents, ..Read(relative, "call_2").Messages[0].Contents,
                ])),
                _ => Say("Read it twice."));

            var (root, spans) = await Trace(Engine(client, new ListUsageSink()), "read it twice", streamed);

            var chats = WithOperation(spans, "chat");
            var tools = WithOperation(spans, "execute_tool");
            var loop = spans.Single(a => a.OperationName == "orchestrate_tools");
            Assert.That(chats, Has.Count.EqualTo(2));
            Assert.That(chats.Select(a => a.Kind), Is.All.EqualTo(ActivityKind.Client));
            Assert.That(tools.Select(a => (string?)a.GetTagItem("gen_ai.tool.name")), Is.All.EqualTo("ReadWorkspaceFile"));
            Assert.That(tools.Select(a => (string?)a.GetTagItem("gen_ai.tool.call.id")), Is.EquivalentTo(new[] { "call_1", "call_2" }));
            Assert.That(chats.Concat(tools).Select(a => a.ParentId), Is.All.EqualTo(loop.Id), "the loop's span holds every request and call");
            Assert.That(loop.ParentId, Is.EqualTo(root.Id));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public async Task Turn_SummaryIsATracedRequestToo()
    {
        string path = Written(new string('x', ToolCallGuards.MaxToolResultChars + 1), out string relative);
        try
        {
            var sink = new ListUsageSink();
            var client = new FakeChatClient(_ => Read(relative, "call_1"), _ => Say("a file full of x"), _ => Say("It's all x."));

            var (_, spans) = await Trace(Engine(client, sink), "what is in the file?", streamed: false);

            var chats = WithOperation(spans, "chat");
            var tool = WithOperation(spans, "execute_tool").Single();
            Assert.That(chats, Has.Count.EqualTo(3));
            Assert.That(chats.Count(a => a.ParentId == tool.Id), Is.EqualTo(1), "the summary's span sits under the call it summarises");
            Assert.That(chats, Has.Count.EqualTo(sink.Records.Count), "the traces and the ledger count the same requests");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public async Task Turn_SpansCarryNoConversation_EvenWithTheCaptureVariableOn()
    {
        Assert.That(new OpenTelemetryChatClient(new FakeChatClient()).EnableSensitiveData, Is.True,
            "the capture variable is on for this process (TelemetryCapture)");

        string[] markers = ["MARKER-USER-WORDS", "MARKER-FILE-NAME", "MARKER-FILE-CONTENT", "MARKER-REPLY"];
        string path = Written("MARKER-FILE-CONTENT" + new string('x', ToolCallGuards.MaxToolResultChars + 1), out string relative, "MARKER-FILE-NAME");
        try
        {
            var client = new FakeChatClient(_ => Read(relative, "call_1"), _ => Say("a summary"), _ => Say("MARKER-REPLY"));

            var (_, spans) = await Trace(Engine(client, new ListUsageSink()), "MARKER-USER-WORDS: what is in the file?", streamed: false);

            Assert.That(WithOperation(spans, "chat"), Is.Not.Empty);
            Assert.That(WithOperation(spans, "execute_tool"), Is.Not.Empty);
            Assert.That(spans.SelectMany(a => a.TagObjects).Select(t => t.Key), Does.Not.Contain(ContentTags[0]).And.Not.Contain(ContentTags[1])
                .And.Not.Contain(ContentTags[2]).And.Not.Contain(ContentTags[3]).And.Not.Contain(ContentTags[4]));
            var values = spans.SelectMany(a => a.TagObjects.Select(t => t.Value)
                .Concat(a.Events.SelectMany(e => e.Tags.Select(t => t.Value)))).Select(v => v?.ToString() ?? "");
            foreach (string marker in markers)
                Assert.That(values, Has.None.Contain(marker), $"no span carries {marker}");
        }
        finally
        {
            File.Delete(path);
        }
    }
}
