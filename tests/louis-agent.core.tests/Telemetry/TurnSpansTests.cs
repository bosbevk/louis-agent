using System.Diagnostics;
using LouisAgent.Choices;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Skills.Providers;
using LouisAgent.Core.Telemetry;
using LouisAgent.Core.Usage.Records;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Tests.Telemetry;

/// <summary>
/// A turn is a span (F52 step 3, ADR 0064): one <c>invoke_agent</c> span per turn, whole or streamed, named for the
/// agent and tagged with the session, host, turn, run and task of its usage scope, holding the tool loop's spans and
/// never a word of the conversation; a failed turn is an error span with the exception on it. The spans are read in
/// the process with a listener, as <c>ChatPipelineTests</c> reads them, so no collector is needed.
/// </summary>
public class TurnSpansTests
{
    /// <summary>The operation tag every GenAI span carries.</summary>
    private const string Operation = "gen_ai.operation.name";

    /// <summary>The engine's name in these tests.</summary>
    private const string Agent = "test-agent";

    /// <summary>The test's own root span, so each test reads only its own trace.</summary>
    private static readonly ActivitySource TestSource = new("louis-agent.core.tests.turn-spans");

    /// <summary>The GenAI tags that would carry the conversation, which no span of ours may hold.</summary>
    private static readonly string[] ContentTags =
    [
        "gen_ai.input.messages", "gen_ai.output.messages", "gen_ai.system_instructions",
        "gen_ai.tool.call.arguments", "gen_ai.tool.call.result",
    ];

    /// <summary>The usage scope the hosts open around a turn.</summary>
    private static UsageScope Scope() =>
        UsageScope.Begin(host: "test", session: "s1", turn: 3, tags: new UsageTags("fix:1", "run-1", null));

    /// <summary>
    /// One turn, one span under the caller's, holding the tool loop; the scope's ids as tags, and nothing of what was
    /// said or read.
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public async Task OneTurn_IsOneSpan_HoldingTheToolLoop(bool streamed)
    {
        string path = Written("MARKER-FILE-CONTENT", out string relative);
        try
        {
            var client = new FakeChatClient(_ => Read(relative, "call_1"), _ => Say("MARKER-REPLY"));

            var (root, spans) = await TraceAsync(Engine(client), "MARKER-USER-WORDS: read it", streamed, Scope);

            var turn = WithOperation(spans, "invoke_agent").Single();
            var calls = WithOperation(spans, "chat").Concat(WithOperation(spans, "execute_tool")).ToList();
            Assert.That((turn.DisplayName, turn.ParentId), Is.EqualTo(($"invoke_agent {Agent}", root.Id)));
            // Under an invoke_agent span the tool loop opens no orchestrate_tools of its own: the turn is the loop.
            Assert.That(spans.Where(a => a.OperationName == "orchestrate_tools"), Is.Empty);
            Assert.That(calls, Has.Count.EqualTo(3), "two model requests and the tool call");
            Assert.That(calls.Select(a => a.ParentId), Is.All.EqualTo(turn.Id));
            Assert.That(Tags(turn), Is.EquivalentTo(new Dictionary<string, object?>
            {
                [SpanTag.OperationName.Wire()] = "invoke_agent",
                [SpanTag.AgentName.Wire()] = Agent,
                [SpanTag.ConversationId.Wire()] = "s1",
                [SpanTag.Host.Wire()] = "test",
                [SpanTag.Turn.Wire()] = 3,
                [SpanTag.Run.Wire()] = "run-1",
                [SpanTag.Task.Wire()] = "fix:1",
            }));
            Assert.That(spans.SelectMany(a => a.TagObjects).Select(t => t.Key).Intersect(ContentTags), Is.Empty);
            var values = spans.SelectMany(a => a.TagObjects.Select(t => t.Value?.ToString() ?? "")).ToList();
            foreach (string marker in (string[])["MARKER-USER-WORDS", "MARKER-FILE-CONTENT", "MARKER-REPLY"])
                Assert.That(values, Has.None.Contain(marker), $"no span carries {marker}");
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// Streamed, the turn's span is current again after every <c>yield</c>: a reply cut off by the output limit is
    /// continued in the same turn, and the continuation's model request sits under the same turn, not at the root.
    /// </summary>
    [Test]
    public async Task Streamed_TheSpanStaysCurrent_AcrossYields()
    {
        var client = new FakeChatClient(_ => Say("half a reply", ChatFinishReason.Length), _ => Say("the rest"));

        var (_, spans) = await TraceAsync(Engine(client), "a long answer, please", streamed: true, Scope);

        var turn = WithOperation(spans, "invoke_agent").Single();
        var requests = WithOperation(spans, "chat");
        Assert.That(requests, Has.Count.EqualTo(2), "the reply and its continuation");
        Assert.That(requests.Select(a => a.ParentId), Is.All.EqualTo(turn.Id));
        Assert.That(turn.IsStopped, Is.True, "the turn's span ended with the stream");
    }

    /// <summary>
    /// A turn that fails is an error span with the exception on it, once; the failure still reaches the host.
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void AFailedTurn_IsAnErrorSpan_WithTheException(bool streamed)
    {
        var client = new FakeChatClient(_ => throw new InvalidOperationException("boom"));
        var spans = new List<Activity>();

        Assert.That(async () => await TraceAsync(Engine(client), "hello", streamed, Scope, spans),
            Throws.InvalidOperationException);

        var turn = WithOperation(spans, "invoke_agent").Single();
        var exceptions = turn.Events.Where(e => e.Name == "exception").ToList();
        Assert.That(turn.Status, Is.EqualTo(ActivityStatusCode.Error));
        Assert.That(turn.StatusDescription, Is.Null, "an exception's message may hold a person's words");
        Assert.That(exceptions, Has.Count.EqualTo(1));
        Assert.That(exceptions[0].Tags.Single(t => t.Key == "exception.type").Value,
            Is.EqualTo(typeof(InvalidOperationException).FullName));
    }

    /// <summary>With no usage scope the turn is still a span, with only the agent's name of ours.</summary>
    [Test]
    public async Task NoScope_StillASpan_WithOnlyTheAgent()
    {
        var client = new FakeChatClient(_ => Say("hi"));

        var (_, spans) = await TraceAsync(Engine(client), "hello", streamed: false, scope: null);

        var turn = WithOperation(spans, "invoke_agent").Single();
        Assert.That(Tags(turn).Keys,
            Is.EquivalentTo(new[] { SpanTag.OperationName.Wire(), SpanTag.AgentName.Wire() }));
    }

    /// <summary>An engine on the fake model with the file tools, named for these tests.</summary>
    private static AgentEngine Engine(IChatClient client) =>
        new(new MarkdownSkillProvider(Path.Combine(TestPaths.RepoRoot, "Skills", "HelloWorld.md")), client,
            TestPaths.Agent(), toolsets: TestPaths.Toolsets(TestPaths.Agent(), [CoreToolset.Files.Wire()]),
            usageSink: new ListUsageSink())
        { Name = Agent };

    /// <summary>The model asks to read one file.</summary>
    private static ChatResponse Read(string relative, string callId) =>
        new(new ChatMessage(ChatRole.Assistant,
        [
            new FunctionCallContent(callId, "ReadWorkspaceFile",
                new Dictionary<string, object?> { ["relativePath"] = relative })
        ]));

    /// <summary>The model says something, and why it stopped.</summary>
    private static ChatResponse Say(string text, ChatFinishReason? finish = null) =>
        new(new ChatMessage(ChatRole.Assistant, text)) { FinishReason = finish };

    /// <summary>A file in the workspace for the model to read; the test deletes it.</summary>
    private static string Written(string content, out string relative)
    {
        relative = $"turn-span-{Guid.NewGuid():N}.txt";
        string path = Path.Combine(TestPaths.RepoRoot, relative);
        File.WriteAllText(path, content);
        return path;
    }

    /// <summary>
    /// Runs a turn under a root span, in the scope given (or none), and returns the spans of that trace only, so other
    /// tests' spans don't mix in; a list given is filled with them even when the turn throws.
    /// </summary>
    private static async Task<(Activity Root, List<Activity> Spans)> TraceAsync(AgentEngine engine, string prompt,
        bool streamed, Func<UsageScope>? scope, List<Activity>? into = null)
    {
        var spans = new List<Activity>();
        var ofTrace = into ?? [];
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
        try
        {
            using var opened = scope?.Invoke();
            if (streamed)
                await foreach (var _ in engine.StreamPromptAsync(engine.NewHistory(), prompt))
                {
                }
            else
                await engine.ProcessPromptAsync(engine.NewHistory(), prompt);
        }
        finally
        {
            lock (spans) ofTrace.AddRange(spans.Where(a => a.TraceId == root.TraceId));
        }

        return (root, ofTrace);
    }

    /// <summary>The spans whose GenAI operation is the one named.</summary>
    private static List<Activity> WithOperation(IEnumerable<Activity> spans, string operation) =>
        spans.Where(a => a.GetTagItem(Operation) as string == operation).ToList();

    /// <summary>A span's tags as a dictionary.</summary>
    private static Dictionary<string, object?> Tags(Activity span) =>
        span.TagObjects.ToDictionary(t => t.Key, t => t.Value);
}
