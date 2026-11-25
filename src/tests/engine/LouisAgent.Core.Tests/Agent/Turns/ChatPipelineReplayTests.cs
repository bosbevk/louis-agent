using LouisAgent.Choices;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Agent.Turns;
using LouisAgent.Core.Pricing.Prices;
using LouisAgent.Core.Sessions.Replay;
using LouisAgent.Core.Skills.Providers;
using LouisAgent.Testing.Replay;
using LouisAgent.Core.Usage.Records;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Tests.Agent.Turns;

/// <summary>
/// The engine replays the model. Two streamed turns with a tool call are recorded, then replayed on a client that
/// throws if the model is asked: the answers must be equal and every ledger row of the replay a known $0 hit. This
/// catches a key that changes between the recording and the replay (the tool loop stamps each tool message with its
/// time and a random id, and the next turn's first request carries them) and a hit tag that leaks into the history
/// (it would change the next request's key).
/// </summary>
public class ChatPipelineReplayTests
{
    private const string Trial = "r1:a:1/t1";
    private ReplayFolder _folder = null!;

    /// <summary>Each test records into a replay folder of its own.</summary>
    [SetUp]
    public void SetUp() => _folder = new ReplayFolder();

    /// <summary>The folder goes when the test is done.</summary>
    [TearDown]
    public void TearDown() => _folder.Dispose();

    /// <summary>
    /// An engine on the model given, its requests priced into the ledger given, replaying from the folder.
    /// </summary>
    private AgentEngine Engine(IChatClient client, ListUsageSink ledger) =>
        new(new MarkdownSkillProvider(TestPaths.SampleSkills), client,
            TestPaths.Agent(),
            toolsets: TestPaths.Toolsets(TestPaths.Agent(), [TestToolset.Files.Wire()]),
            // Priced the way the ledger is, from a table that knows no model: a hit must still be a known 0, and a
            // live request unknown.
            usageSink: new PricingUsageSink(ledger, new PriceTable
            {
                AsOf = "2026-10-25",
                Currency = "USD"
            }),
            replay: _folder.Store);

    /// <summary>The model's answer that reads a workspace file.</summary>
    private static ChatResponse Read(string relative) =>
        new(new ChatMessage(ChatRole.Assistant,
        [
            new FunctionCallContent("call_1", "ReadWorkspaceFile",
                new Dictionary<string, object?> { ["relativePath"] = relative })
        ]));

    /// <summary>The model's answer in plain text.</summary>
    private static ChatResponse Say(string text) => new(new ChatMessage(ChatRole.Assistant, text));

    /// <summary>Turns replay on for the web and, if asked, the model, until disposed.</summary>
    private static IDisposable SetReplay(WebReplayMode web, bool model) =>
        SessionReplay.Use(new SessionReplay(web, Trial, model));

    /// <summary>Two streamed turns on one history; returns each turn's streamed text and the history.</summary>
    private static async Task<(string[] Texts, Conversation History)> TwoTurns(AgentEngine engine)
    {
        var history = engine.StartConversation();
        var texts = new List<string>();
        foreach (string prompt in new[] { "what is in the file?", "thanks" })
        {
            var text = new System.Text.StringBuilder();
            using (UsageScope.Begin(host: "test", session: "s1", turn: texts.Count + 1))
                await foreach (var update in engine.StreamPromptAsync(history, prompt))
                    text.Append(update.Text);
            texts.Add(text.ToString());
        }

        return ([.. texts], history);
    }

    /// <summary>A file of x's the length given in the repo's root, and its path from there.</summary>
    private static string FileOf(int length, out string relative)
    {
        relative = $"replay-{Guid.NewGuid():N}.txt";
        string path = Path.Combine(TestPaths.RepoRoot, relative);
        File.WriteAllText(path, new string('x', length));
        return path;
    }

    /// <summary>
    /// Two turns with a tool call replay at no cost and answer the same, the summary of an oversized result too.
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public async Task TwoStreamedTurnsWithAToolCall_ReplayFree_AndEqual(bool oversizedResult)
    {
        // An oversized result is summarised by a second model request; that summary must replay too, or a free replay
        // would pay for it.
        string path = FileOf(oversizedResult ? ToolCallGuards.MaxToolResultChars + 1 : 20, out string relative);
        try
        {
            var live = oversizedResult
                ? new FakeChatClient(_ => Read(relative), _ => Say("a file full of x"), _ => Say("It's all x."),
                    _ => Say("You're welcome."))
                : new FakeChatClient(_ => Read(relative), _ => Say("It's all x."), _ => Say("You're welcome."));
            var recording = new ListUsageSink();
            string[] recorded;
            using (SetReplay(WebReplayMode.Record, model: false))
                (recorded, _) = await TwoTurns(Engine(live, recording));

            var throwing = new FakeChatClient(_ => throw new InvalidOperationException("the model was called"));
            var replaying = new ListUsageSink();
            string[] replayed;
            Conversation history;
            using (SetReplay(WebReplayMode.Replay, model: true))
                (replayed, history) = await TwoTurns(Engine(throwing, replaying));

            Assert.That(replayed, Is.EqualTo(recorded));
            Assert.That(throwing.Requests, Is.Empty);
            Assert.That(replaying.Records, Has.Count.EqualTo(recording.Records.Count),
                "the same requests, each one a row");
            Assert.That(replaying.Records,
                Has.All.Matches<UsageRecord>(r =>
                    r.CacheHit == true && r.Cost == new UsageCost("USD", 0m, "2026-10-25")));
            Assert.That(recording.Records, Has.All.Matches<UsageRecord>(r => r.CacheHit is null && r.Cost is null),
                "a live request on an unpriced model stays unknown");
            Assert.That(history.Messages.SelectMany(m => m.AdditionalProperties?.Keys ?? []),
                Does.Not.Contain("replay_hit"),
                "the tag never enters the history (ADR 0008)");
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>An engine with a replay store but no replay switched on neither reads nor writes it.</summary>
    [Test]
    public async Task AnEngineWithAStoreButNoReplayInTheFlow_NeitherReadsNorWrites()
    {
        var live = new FakeChatClient(_ => Say("one"), _ => Say("two"));

        string[] texts;
        (texts, _) = await TwoTurns(Engine(live, new ListUsageSink()));

        Assert.That(texts, Is.EqualTo(new[] { "one", "two" }));
        Assert.That(live.Requests, Has.Count.EqualTo(2));
        Assert.That(_folder.Entries, Is.Empty);
    }

    /// <summary>Replaying only the web asks the live model, and records nothing of it.</summary>
    [Test]
    public async Task AWebReplayWithoutTheModelFlag_RunsOnALiveModel_AndWritesNothing()
    {
        var live = new FakeChatClient(_ => Say("one"), _ => Say("two"));

        using (SetReplay(WebReplayMode.Replay, model: false)) await TwoTurns(Engine(live, new ListUsageSink()));

        Assert.That(live.Requests, Has.Count.EqualTo(2));
        Assert.That(_folder.Entries, Is.Empty);
    }
}
