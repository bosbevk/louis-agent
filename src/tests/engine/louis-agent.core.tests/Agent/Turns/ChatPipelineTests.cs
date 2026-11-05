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

/// <summary>
/// A turn's trace (F18 step 4d): a span per model request and per tool call, under the turn's own span (F52 step 3),
/// with nothing of the conversation on them. The spans are Microsoft.Extensions.AI's own, named by the OpenTelemetry
/// GenAI conventions (the span name is "{gen_ai.operation.name} {target}", so the tests read the operation tag, not
/// the name); an <see cref="ActivityListener"/> stands in for an exporter.
/// </summary>
public class ChatPipelineTests
{
    /// <summary>The operation tag every GenAI span carries.</summary>
    private const string Operation = "gen_ai.operation.name";

    /// <summary>The test's own root span, so each test reads only its own trace.</summary>
    private static readonly ActivitySource TestSource = new("louis-agent.core.tests.chat-pipeline");

    /// <summary>The GenAI tags that would carry the conversation, which no span may hold.</summary>
    private static readonly string[] ContentTags =
    [
        "gen_ai.input.messages", "gen_ai.output.messages", "gen_ai.system_instructions",
        "gen_ai.tool.call.arguments", "gen_ai.tool.call.result",
    ];

    /// <summary>An engine on the fake model with the file tools, recording into the sink given.</summary>
    private static AgentEngine Engine(IChatClient client, IUsageSink sink) =>
        new(new MarkdownSkillProvider(TestPaths.SampleSkills), client,
            TestPaths.Agent(),
            toolsets: TestPaths.Toolsets(TestPaths.Agent(), [TestToolset.Files.Wire()]), usageSink: sink);

    /// <summary>The model asks to read one file.</summary>
    private static ChatResponse Read(string relative, string callId) =>
        new(new ChatMessage(ChatRole.Assistant,
        [
            new FunctionCallContent(callId, "ReadWorkspaceFile",
                new Dictionary<string, object?> { ["relativePath"] = relative })
        ]));

    /// <summary>The model says something.</summary>
    private static ChatResponse Say(string text) => new(new ChatMessage(ChatRole.Assistant, text));

    /// <summary>
    /// Runs a turn under a root span and returns the spans of that trace only, so parallel tests don't mix.
    /// </summary>
    private static async Task<(Activity Root, List<Activity> Spans)> Trace(AgentEngine engine, string prompt,
        bool streamed)
    {
        var spans = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = a =>
            {
                lock (spans) spans.Add(a);
            },
        };
        ActivitySource.AddActivityListener(listener);

        using var root = TestSource.StartActivity("test turn")!;
        using (UsageScope.Begin())
        {
            if (streamed)
                await foreach (var _ in engine.StreamPromptAsync(engine.NewHistory(), prompt))
                {
                }
            else
                await engine.ProcessPromptAsync(engine.NewHistory(), prompt);
        }

        lock (spans) return (root, spans.Where(a => a.TraceId == root.TraceId).ToList());
    }

    /// <summary>The spans whose GenAI operation is the one named.</summary>
    private static List<Activity> WithOperation(IEnumerable<Activity> spans, string operation) =>
        spans.Where(a => a.GetTagItem(Operation) as string == operation).ToList();

    /// <summary>A file in the workspace for the model to read; the test deletes it.</summary>
    private static string Written(string content, out string relative, string prefix = "trace")
    {
        relative = $"{prefix}-{Guid.NewGuid():N}.txt";
        string path = Path.Combine(TestPaths.RepoRoot, relative);
        File.WriteAllText(path, content);
        return path;
    }

    /// <summary>
    /// One span per model request and one per tool call, every one under the turn's span, whole and streamed.
    /// </summary>
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
                    .. Read(relative, "call_1").Messages[0].Contents, .. Read(relative, "call_2").Messages[0].Contents,
                ])),
                _ => Say("Read it twice."));

            var (root, spans) = await Trace(Engine(client, new ListUsageSink()), "read it twice", streamed);

            var chats = WithOperation(spans, "chat");
            var tools = WithOperation(spans, "execute_tool");
            // The turn's own span (F52 step 3) is the loop: under an invoke_agent, MEAI opens no orchestrate_tools.
            var loop = WithOperation(spans, "invoke_agent").Single();
            Assert.That(chats, Has.Count.EqualTo(2));
            Assert.That(chats.Select(a => a.Kind), Is.All.EqualTo(ActivityKind.Client));
            Assert.That(tools.Select(a => (string?)a.GetTagItem("gen_ai.tool.name")),
                Is.All.EqualTo("ReadWorkspaceFile"));
            Assert.That(tools.Select(a => (string?)a.GetTagItem("gen_ai.tool.call.id")),
                Is.EquivalentTo(new[] { "call_1", "call_2" }));
            Assert.That(chats.Concat(tools).Select(a => a.ParentId), Is.All.EqualTo(loop.Id),
                "the loop's span holds every request and call");
            Assert.That(loop.ParentId, Is.EqualTo(root.Id));
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// A summary of an oversized result is a model request too: its span sits under the call it summarises, and the
    /// traces and the ledger count the same requests.
    /// </summary>
    [Test]
    public async Task Turn_SummaryIsATracedRequestToo()
    {
        string path = Written(new string('x', ToolCallGuards.MaxToolResultChars + 1), out string relative);
        try
        {
            var sink = new ListUsageSink();
            var client = new FakeChatClient(_ => Read(relative, "call_1"), _ => Say("a file full of x"),
                _ => Say("It's all x."));

            var (_, spans) = await Trace(Engine(client, sink), "what is in the file?", streamed: false);

            var chats = WithOperation(spans, "chat");
            var tool = WithOperation(spans, "execute_tool").Single();
            Assert.That(chats, Has.Count.EqualTo(3));
            Assert.That(chats.Count(a => a.ParentId == tool.Id), Is.EqualTo(1),
                "the summary's span sits under the call it summarises");
            Assert.That(chats, Has.Count.EqualTo(sink.Records.Count),
                "the traces and the ledger count the same requests");
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// No span carries a word of the conversation, the file's name and content included, even with the capture
    /// variable on for the whole process.
    /// </summary>
    [Test]
    public async Task Turn_SpansCarryNoConversation_EvenWithTheCaptureVariableOn()
    {
        Assert.That(new OpenTelemetryChatClient(new FakeChatClient()).EnableSensitiveData, Is.True,
            "the capture variable is on for this process (TelemetryCapture)");

        string[] markers = ["MARKER-USER-WORDS", "MARKER-FILE-NAME", "MARKER-FILE-CONTENT", "MARKER-REPLY"];
        string path = Written("MARKER-FILE-CONTENT" + new string('x', ToolCallGuards.MaxToolResultChars + 1),
            out string relative, "MARKER-FILE-NAME");
        try
        {
            var client = new FakeChatClient(_ => Read(relative, "call_1"), _ => Say("a summary"),
                _ => Say("MARKER-REPLY"));

            var (_, spans) = await Trace(Engine(client, new ListUsageSink()), "MARKER-USER-WORDS: what is in the file?",
                streamed: false);

            Assert.That(WithOperation(spans, "chat"), Is.Not.Empty);
            Assert.That(WithOperation(spans, "execute_tool"), Is.Not.Empty);
            Assert.That(spans.SelectMany(a => a.TagObjects).Select(t => t.Key), Does.Not.Contain(ContentTags[0]).And.Not
                .Contain(ContentTags[1])
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
