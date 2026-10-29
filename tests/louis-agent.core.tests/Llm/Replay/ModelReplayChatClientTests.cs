using LouisAgent.Core.Llm.Replay;
using LouisAgent.Core.Sessions.Replay;
using LouisAgent.Core.Tests.Tools.Web.Replay;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Tests.Llm.Replay;

/// <summary>
/// The model's answers recorded and replayed under the session's trial (F18 step 4e.2). Each case runs streamed and
/// not (the client stores both ways as one response). The inner client is a counting fake: a hit and a miss must
/// never reach it, and a miss is the named <see cref="ModelReplayMissException"/> (FX26).
/// </summary>
public class ModelReplayChatClientTests
{
    /// <summary>The trial the tests record into and replay from.</summary>
    private const string Trial = "r1:a:1/t1";

    /// <summary>A fresh replay folder for each test.</summary>
    private ReplayFolder _folder = null!;

    /// <summary>Makes the test's replay folder.</summary>
    [SetUp]
    public void SetUp() => _folder = new ReplayFolder();

    /// <summary>Deletes the test's replay folder.</summary>
    [TearDown]
    public void TearDown() => _folder.Dispose();

    /// <summary>A model's reply with this text, and its usage if given.</summary>
    private static ChatResponse Reply(string text, UsageDetails? usage = null) =>
        new(new ChatMessage(ChatRole.Assistant, text))
        {
            Usage = usage,
            ModelId = "m1",
            FinishReason = ChatFinishReason.Stop
        };

    /// <summary>A model that answers these texts in turn, counting what it was asked.</summary>
    private static FakeChatClient Fake(params string[] texts) =>
        new([.. texts.Select(t => (Func<IList<ChatMessage>, ChatResponse>)(_ => Reply(t)))]);

    /// <summary>
    /// A provider that reports its usage the way a real one does: on the response, and as a usage update when
    /// streamed.
    /// </summary>
    private sealed class ReportsUsage(string text, UsageDetails usage) : IChatClient
    {
        /// <summary>The reply with its usage on it.</summary>
        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Reply(text, usage));

        /// <summary>The text, then the usage as its own update, then the stop.</summary>
        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation]
            CancellationToken cancellationToken = default)
        {
            yield return new ChatResponseUpdate(ChatRole.Assistant, text) { ModelId = "m1" };
            yield return new ChatResponseUpdate(ChatRole.Assistant, [new UsageContent(usage)]);
            yield return new ChatResponseUpdate { FinishReason = ChatFinishReason.Stop };
            await Task.CompletedTask;
        }

        /// <summary>No services.</summary>
        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        /// <summary>Nothing to let go of.</summary>
        public void Dispose()
        {
        }
    }

    /// <summary>A model that fails the test if it is ever called.</summary>
    private static FakeChatClient Throwing() => new(_ => throw new InvalidOperationException("the model was called"));

    /// <summary>The replay client over this inner model and the test's store.</summary>
    private ModelReplayChatClient Client(IChatClient inner) => new(inner, _folder.Store);

    /// <summary>The session's replay for the duration of a block.</summary>
    private static IDisposable Flow(WebReplayMode web, bool model, string trial = Trial) =>
        SessionReplay.Use(new SessionReplay(web, trial, model));

    /// <summary>The request's options: the model named.</summary>
    private static readonly ChatOptions Options = new() { ModelId = "m1" };

    /// <summary>A two-message chat: the system prompt and the user's question.</summary>
    private static List<ChatMessage> Chat(string user = "hello") =>
        [new(ChatRole.System, "You are Puls."), new(ChatRole.User, user)];

    /// <summary>The answer, and whether any update (or the response) carried the hit tag.</summary>
    private static async Task<(ChatResponse Response, bool Tagged)> Ask(IChatClient client, bool streamed,
        IEnumerable<ChatMessage> messages)
    {
        if (!streamed)
        {
            ChatResponse response = await client.GetResponseAsync(messages, Options);
            return (response, response.AdditionalProperties?.ContainsKey("replay_hit") == true);
        }

        var updates = new List<ChatResponseUpdate>();
        await foreach (ChatResponseUpdate update in client.GetStreamingResponseAsync(messages, Options))
            updates.Add(update);
        return (updates.ToChatResponse(), updates.Any(u => u.AdditionalProperties?.ContainsKey("replay_hit") == true));
    }

    /// <summary>The store's lines that hold a model answer.</summary>
    private string[] ModelLines() =>
        _folder.Trials.SelectMany(File.ReadAllLines).Where(l => l.Contains("\"kind\":\"model\"")).ToArray();

    /// <summary>Recording calls the model and saves its answer, and never reads an earlier recording.</summary>
    [TestCase(false)]
    [TestCase(true)]
    public async Task Record_CallsTheModel_SavesTheAnswer_AndNeverReads(bool streamed)
    {
        // A recording of the same request is already there; a recording run must not read it (pass^k needs
        // independent trials).
        using (Flow(WebReplayMode.Record, model: false))
            await Ask(Client(Fake("old")), streamed, Chat());
        var live = Fake("new");

        using (Flow(WebReplayMode.Record, model: false))
        {
            var (response, tagged) = await Ask(Client(live), streamed, Chat());

            Assert.That(response.Text, Is.EqualTo("new"));
            Assert.That(tagged, Is.False);
        }

        Assert.That(live.Requests, Has.Count.EqualTo(1));
        Assert.That(ModelLines(), Has.Length.EqualTo(2));
    }

    /// <summary>With no replay, the web off, or the model not replayed, the client stays out of the way.</summary>
    [TestCase(false)]
    [TestCase(true)]
    public async Task WithoutAReplayOrWebOffOrWithoutTheModelFlag_ItNeitherReadsNorWrites(bool streamed)
    {
        var none = Fake("a");
        await Ask(Client(none), streamed, Chat());
        var off = Fake("b");
        using (Flow(WebReplayMode.Off, model: true))
            await Ask(Client(off), streamed, Chat());
        var webOnly = Fake("c");
        using (Flow(WebReplayMode.Replay, model: false))
            Assert.That((await Ask(Client(webOnly), streamed, Chat())).Response.Text, Is.EqualTo("c"));

        Assert.That(new[] { none.Requests.Count, off.Requests.Count, webOnly.Requests.Count }, Is.All.EqualTo(1));
        Assert.That(_folder.Entries, Is.Empty);
    }

    /// <summary>A hit answers from the recording, tagged, with no usage, and never calls the model.</summary>
    [TestCase(false)]
    [TestCase(true)]
    public async Task AHit_NeverCallsTheModel_ReturnsTheRecordedText_TaggedAndWithNoUsage(bool streamed)
    {
        var usage = new UsageDetails
        {
            InputTokenCount = 100,
            OutputTokenCount = 20
        };
        using (Flow(WebReplayMode.Record, model: false))
            await Ask(Client(new ReportsUsage("recorded answer", usage)), streamed, Chat());

        var throwing = Throwing();
        using (Flow(WebReplayMode.Replay, model: true))
        {
            var (response, tagged) = await Ask(Client(throwing), streamed, Chat());

            Assert.That(response.Text, Is.EqualTo("recorded answer"));
            Assert.That(tagged, Is.True);
            Assert.That(response.Usage, Is.Null, "no layer above may count the recorded tokens");
        }

        Assert.That(throwing.Requests, Is.Empty);
    }

    /// <summary>The same request in two trials replays each trial's own answer.</summary>
    [TestCase(false)]
    [TestCase(true)]
    public async Task TheSameRequestInTwoTrials_ReplaysEachTrialsOwnAnswer(bool streamed)
    {
        using (Flow(WebReplayMode.Record, model: false, "r1:a:1/t1"))
            await Ask(Client(Fake("first")), streamed, Chat());
        using (Flow(WebReplayMode.Record, model: false, "r1:a:2/t1"))
            await Ask(Client(Fake("second")), streamed, Chat());

        using (Flow(WebReplayMode.Replay, model: true, "r1:a:2/t1"))
            Assert.That((await Ask(Client(Throwing()), streamed, Chat())).Response.Text, Is.EqualTo("second"));
        using (Flow(WebReplayMode.Replay, model: true, "r1:a:1/t1"))
            Assert.That((await Ask(Client(Throwing()), streamed, Chat())).Response.Text, Is.EqualTo("first"));
    }

    /// <summary>
    /// A miss never calls the model, and throws the named miss (FX26) saying what changed in the request.
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public async Task AMiss_NeverCallsTheModel_AndNamesTheChange(bool streamed)
    {
        using (Flow(WebReplayMode.Record, model: false))
            await Ask(Client(Fake("recorded")), streamed, Chat("hello"));

        var throwing = Throwing();
        using (Flow(WebReplayMode.Replay, model: true))
        {
            var thrown = Assert.ThrowsAsync<ModelReplayMissException>(async () =>
                await Ask(Client(throwing), streamed, Chat("another question")));

            Assert.That(thrown!.Message, Does.Contain(Trial).And.Contain("message 1 (user) changed"));
        }

        Assert.That(throwing.Requests, Is.Empty);
    }

    /// <summary>A replay of a trial recorded before the model was, is a miss that says so.</summary>
    [TestCase(false)]
    [TestCase(true)]
    public async Task AReplayOfATrialWithNoModelAnswers_SaysSo(bool streamed)
    {
        using (Flow(WebReplayMode.Replay, model: true))
        {
            var thrown =
                Assert.ThrowsAsync<ModelReplayMissException>(async () =>
                    await Ask(Client(Throwing()), streamed, Chat()));

            Assert.That(thrown!.Message, Does.Contain("recorded before step 4e"));
        }
    }

    /// <summary>
    /// A thinking block replays byte for byte, so the next request, which carries it in the history, hits too.
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public async Task Thinking_SurvivesByteEqual_AndTheNextRequestWithTheReplayedReplyHitsToo(bool streamed)
    {
        var reply = new ChatResponse(new ChatMessage(ChatRole.Assistant,
        [
            new TextReasoningContent("let me think") { ProtectedData = "c2lnbmF0dXJl==" },
            new TextContent("the answer")
        ]))
        {
            ModelId = "m1",
            FinishReason = ChatFinishReason.Stop
        };
        var second = Reply("second answer");

        // Recording: turn 1, then turn 2 carrying turn 1's reply (and stamped as the tool loop stamps its messages).
        List<ChatMessage> turn2Live;
        using (Flow(WebReplayMode.Record, model: false))
        {
            var client = Client(new FakeChatClient(_ => reply, _ => second));
            var (live, _) = await Ask(client, streamed, Chat());
            turn2Live =
            [
                .. Chat(), .. live.Messages,
                new ChatMessage(ChatRole.User, "and then?")
                {
                    CreatedAt = DateTimeOffset.UnixEpoch,
                    MessageId = "live"
                }
            ];
            await Ask(client, streamed, turn2Live);
        }

        var throwing = Throwing();
        using (Flow(WebReplayMode.Replay, model: true))
        {
            var client = Client(throwing);
            var (replayed, _) = await Ask(client, streamed, Chat());

            var thinking = replayed.Messages[0].Contents.OfType<TextReasoningContent>().Single();
            Assert.That((thinking.Text, thinking.ProtectedData), Is.EqualTo(("let me think", "c2lnbmF0dXJl==")));

            List<ChatMessage> turn2 =
            [
                .. Chat(), .. replayed.Messages,
                new ChatMessage(ChatRole.User, "and then?")
                {
                    CreatedAt = DateTimeOffset.UtcNow,
                    MessageId = "replay"
                }
            ];
            Assert.That((await Ask(client, streamed, turn2)).Response.Text, Is.EqualTo("second answer"));
        }

        Assert.That(throwing.Requests, Is.Empty);
    }
}
