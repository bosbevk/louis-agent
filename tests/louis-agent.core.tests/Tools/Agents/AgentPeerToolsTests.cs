using System.Net;
using System.Text;
using System.Text.Json;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Config;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Tools.Agents;
using LouisAgent.Core.Tools.Clock;
using LouisAgent.Core.Usage;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Tests.Tools.Agents;

// F5 step 8 (F5-S5): a sub-session on a peer, with its limits in code. The peer is a stub: no network.
public class AgentPeerToolsTests
{
    private static readonly Dictionary<string, Uri> Peers = new() { ["coder"] = new Uri("http://coder.test/") };

    private const string Stream = """
        event: message_start
        data: {"type":"message_start","message":{"session_id":"sess_child"}}

        event: content_block_delta
        data: {"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"Fixed the null check; "}}

        event: content_block_delta
        data: {"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"14/14 tests pass."}}

        event: message_delta
        data: {"type":"message_delta","delta":{"stop_reason":"end_turn"},"usage":{"input_tokens":1000,"cache_creation_input_tokens":0,"cache_read_input_tokens":9000,"output_tokens":200,"requests":3,"cost":0.0123,"currency":"USD"}}

        event: message_stop
        data: {"type":"message_stop"}

        """;

    private static HttpResponseMessage Json(HttpStatusCode status, string json) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    /// <summary>A peer that creates sess_child, answers with <paramref name="answer"/> and accepts the delete.</summary>
    private static StubHandler Peer(Func<HttpResponseMessage>? answer = null) => new(request => request.Method.Method switch
    {
        "POST" when request.RequestUri!.AbsolutePath == "/sessions" => Json(HttpStatusCode.Created, """{"session_id":"sess_child"}"""),
        "POST" => answer?.Invoke() ?? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Stream, Encoding.UTF8, "text/event-stream") },
        "DELETE" => Json(HttpStatusCode.OK, """{"deleted":true}"""),
        _ => new HttpResponseMessage(HttpStatusCode.NotFound),
    });

    private static AgentPeerTools Tools(StubHandler handler, TimeSpan? timeout = null, TimeProvider? time = null, IUsageSink? sink = null) =>
        new(Peers, "the-key", new HttpClient(handler), timeout, time, sink);

    /// <summary>A reply that failed after some work: the API's error event carries what was charged (FX6).</summary>
    private const string FailedStream = """
        event: error
        data: {"type":"error","error":{"type":"api_error","message":"the model refused"},"usage":{"input_tokens":500,"cache_creation_input_tokens":0,"cache_read_input_tokens":0,"output_tokens":50,"requests":2,"cost":0.002,"currency":"USD"}}

        """;

    [Test]
    public async Task ASubSessionsUsage_IsRolledUpIntoTheParentTurn_AsOneLedgerRow()
    {
        // F5 follow-up: the specialist's 3 requests count in the parent's turn (its usage line, chat total, /usage,
        // GET /sessions/{id} all total the scope) and leave one sub_session row in the parent's ledger.
        var sink = new ListUsageSink();
        var handler = Peer();

        UsageTotal total;
        using (var scope = UsageScope.Begin(host: "api", session: "sess_parent", turn: 2))
        {
            await Tools(handler, sink: sink).StartSubSession(["files", "git"], "Fix order 1006.", agent: "coder");
            total = scope.Total();
        }

        Assert.That((total.Requests, total.Input, total.CacheRead, total.Output, total.Cost), Is.EqualTo((3, 1000L, 9000L, 200L, (decimal?)0.0123m)));
        UsageRecord row = sink.Records.Single();
        Assert.That((row.Purpose, row.Model, row.Stop, row.Round, row.Requests), Is.EqualTo(("sub_session", "sub-session:coder", "sub_session", 0, (int?)3)));
        Assert.That((row.Host, row.Session, row.Turn), Is.EqualTo(("api", "sess_parent", (int?)2)));
        Assert.That(row.Cost, Is.EqualTo(new UsageCost("USD", 0.0123m, null)), "what the child was charged, not a display amount");
        Assert.That(row.Tokens, Is.EqualTo(new UsageTokens(1000, 0, 9000, 200, null)));
    }

    [Test]
    public async Task TheFollowUpAnswer_IsASecondRollUp()
    {
        // FX11's follow-up is a second reply in the same sub-session: its 11 + 3 requests are two rows and one total.
        var sink = new ListUsageSink();

        UsageTotal total;
        using (var scope = UsageScope.Begin(session: "sess_parent", turn: 1))
        {
            await Tools(Peer(Replies(EmptyStream, Stream)), sink: sink).StartSubSession(["files"], "x", agent: "coder");
            total = scope.Total();
        }

        Assert.That(sink.Records.Select(r => r.Requests), Is.EqualTo(new int?[] { 11, 3 }));
        Assert.That((total.Requests, total.Cost), Is.EqualTo((14, (decimal?)0.0204m)));
    }

    [Test]
    public async Task AFailedChild_StillRollsUpWhatItWasCharged()
    {
        var sink = new ListUsageSink();

        string result;
        using (var scope = UsageScope.Begin(session: "sess_parent", turn: 1))
            result = await Tools(Peer(Replies(FailedStream)), sink: sink).StartSubSession(["files"], "x", agent: "coder");

        Assert.That(result, Does.StartWith("Error").And.Contain("the model refused"));
        Assert.That((sink.Records.Single().Requests, sink.Records.Single().Cost?.Amount), Is.EqualTo(((int?)2, (decimal?)0.002m)));
    }

    [Test]
    public async Task ATimeout_RollsUpNothing()
    {
        // No usage arrived, so there is nothing to count: never a zero record (trap 4).
        var sink = new ListUsageSink();

        using (UsageScope.Begin(session: "sess_parent", turn: 1))
            await new AgentPeerTools(Peers, null, new HttpClient(new SlowPeer()), TimeSpan.FromMilliseconds(200), usageSink: sink)
                .StartSubSession(["files"], "x", agent: "coder");

        Assert.That(sink.Records, Is.Empty);
    }

    [Test]
    public async Task ThroughTheEngine_TheLedgerHasTheParentsRowsAndOneRollUp_AndNoneOfTheChilds()
    {
        // Step 3: a turn that delegates writes its own two requests and one roll-up, in the order they happened; the
        // specialist's own rows are written where it ran. The turn's total is own + child, with one adder.
        var sink = new ListUsageSink();
        var tools = new AgentPeerTools(Peers, null, new HttpClient(Peer()), usageSink: sink);
        static ChatResponse Reply(params AIContent[] contents) => new(new ChatMessage(ChatRole.Assistant, contents))
        {
            Usage = new UsageDetails { InputTokenCount = 1_000, OutputTokenCount = 100 },
        };
        var engine = new AgentEngine(
            new MarkdownSkillProvider(Path.Combine(TestPaths.RepoRoot, "Skills", "HelloWorld.md")),
            new FakeChatClient(
                _ => Reply(new FunctionCallContent("call_1", "StartSubSession",
                    new Dictionary<string, object?> { ["toolsets"] = new[] { "files" }, ["message"] = "Fix order 1006.", ["agent"] = "coder" })),
                _ => Reply(new TextContent("Done."))),
            TestPaths.Agent(), toolsets: [tools], usageSink: sink);

        UsageTotal total;
        using (var scope = UsageScope.Begin(host: "api", session: "sess_parent", turn: 1))
        {
            await engine.ProcessPromptAsync(engine.NewHistory(), "delegate this");
            total = scope.Total();
        }

        Assert.That(sink.Records.Select(r => r.Purpose), Is.EqualTo(new[] { "turn", "sub_session", "turn" }));
        Assert.That(sink.Records.Select(r => r.Session), Is.All.EqualTo("sess_parent"));
        Assert.That((total.Requests, total.Cost, total.Unpriced), Is.EqualTo((5, (decimal?)0.0123m, 2)), "1 + 3 + 1; the own rows are unpriced here");
    }

    [Test]
    public async Task OutsideATurn_TheRollUpIsStillALedgerRow()
    {
        // As a model request outside a scope is (FX5): the ledger records it with no host or session.
        var sink = new ListUsageSink();

        string result = await Tools(Peer(), sink: sink).StartSubSession(["files"], "x", agent: "coder");

        Assert.That(result, Does.StartWith("Fixed the null check"));
        Assert.That((sink.Records.Single().Session, sink.Records.Single().Requests), Is.EqualTo(((string?)null, (int?)3)));
    }

    [Test]
    public async Task ASubSession_ReturnsTheAnswerAndUsage_AndIsDeleted()
    {
        var handler = Peer();

        string result = await Tools(handler).StartSubSession(["files", "git"], "Fix order 1006.", agent: "coder");

        Assert.That(result, Does.StartWith("Fixed the null check; 14/14 tests pass."));
        Assert.That(result, Does.Contain("[Sub-session on coder (files, git): 3 requests, 10,200 tokens, 0.0123 USD]"));
        Assert.That(handler.Requests.Select(r => $"{r.Method} {new Uri(r.Url).AbsolutePath}"),
            Is.EqualTo(new[] { "POST /sessions", "POST /sessions/sess_child/messages", "DELETE /sessions/sess_child" }));
        using var create = JsonDocument.Parse(handler.Requests[0].Body!);
        Assert.That(create.RootElement.GetProperty("depth").GetInt32(), Is.EqualTo(1), "one level deep");
        Assert.That(create.RootElement.GetProperty("toolsets").EnumerateArray().Select(t => t.GetString()), Is.EqualTo(new[] { "files", "git" }));
        Assert.That(create.RootElement.GetProperty("personality").ValueKind, Is.EqualTo(JsonValueKind.Null), "none named: the peer's default");
    }

    /// <summary>A peer whose sessions get their own ids (sess_1, sess_2, …), so kept-open ones can be told apart.</summary>
    private static StubHandler PeerWithIds()
    {
        int created = 0;
        return new StubHandler(request => request.Method.Method switch
        {
            "POST" when request.RequestUri!.AbsolutePath == "/sessions" => Json(HttpStatusCode.Created, $$"""{"session_id":"sess_{{++created}}"}"""),
            "POST" => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Stream, Encoding.UTF8, "text/event-stream") },
            "DELETE" => Json(HttpStatusCode.OK, """{"deleted":true}"""),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound),
        });
    }

    private static IEnumerable<string> Calls(StubHandler handler) => handler.Requests.Select(r => $"{r.Method} {new Uri(r.Url).AbsolutePath}");

    [Test]
    public async Task InAChat_TheSubSessionIsKept_AndTheAnswerNamesItsHandle()
    {
        // F26 step 1: with the chat's registry current, the specialist stays open for a follow-up, and the footer tells
        // the model how to continue it. Without a registry (an MCP call), it is deleted as before.
        var handler = Peer();
        var registry = new SubSessionRegistry();

        string result;
        using (SubSessionRegistry.Use(registry))
            result = await Tools(handler).StartSubSession(["web"], "Wat kan ek vanaand kook?", agent: "coder", personality: "puls-cook");

        Assert.That(result, Does.StartWith("Fixed the null check; 14/14 tests pass."));
        Assert.That(result, Does.Contain("[Sub-session sub-1 on coder (web; puls-cook): 3 requests, 10,200 tokens, 0.0123 USD")
            .And.Contain("ContinueSubSession(\"sub-1\""));
        Assert.That(Calls(handler), Is.EqualTo(new[] { "POST /sessions", "POST /sessions/sess_child/messages" }), "no DELETE");
        OpenSubSession open = registry.Open.Single();
        Assert.That((open.Handle, open.SessionId, open.Agent, open.Role), Is.EqualTo(("sub-1", "sess_child", "coder", "puls-cook")));
        Assert.That(open.Toolsets, Is.EqualTo(new[] { "web" }));
    }

    [Test]
    public async Task AFailedStart_IsNotKept()
    {
        var handler = Peer(Replies(FailedStream));
        var registry = new SubSessionRegistry();

        using (SubSessionRegistry.Use(registry))
            await Tools(handler).StartSubSession(["files"], "x", agent: "coder");

        Assert.That(handler.Requests.Last().Method, Is.EqualTo(HttpMethod.Delete));
        Assert.That(registry.Open, Is.Empty);
    }

    [Test]
    public async Task PastTheChatsCap_TheOldestKeptSubSession_IsDeletedOnThePeer()
    {
        // F26-S3: the registry evicts; the tool's closer sends the DELETE with the API key.
        var handler = PeerWithIds();
        var registry = new SubSessionRegistry(cap: 2);
        var tools = Tools(handler);

        using (SubSessionRegistry.Use(registry))
        using (UsageScope.Begin(session: "sess_parent", turn: 1))
        {
            for (int i = 0; i < 3; i++)
                Assert.That(await tools.StartSubSession(["files"], "x", agent: "coder"), Does.Not.StartWith("Error"));
        }

        Assert.That(Calls(handler).Where(c => c.StartsWith("DELETE")), Is.EqualTo(new[] { "DELETE /sessions/sess_1" }));
        Assert.That(registry.Open.Select(o => o.SessionId), Is.EqualTo(new[] { "sess_2", "sess_3" }));
    }

    [Test]
    public async Task AContinue_GoesToTheKeptSession_WhichStaysOpen_AndRollsUpLikeAFirstAnswer()
    {
        // F26 step 2 (F26-S2): the follow-up is a second message in the same peer session: its context and cached
        // prefix stay. The footer names the handle again, so the model knows it's still open.
        var handler = Peer();
        var registry = new SubSessionRegistry();
        var sink = new ListUsageSink();
        var tools = Tools(handler, sink: sink);

        string result;
        using (SubSessionRegistry.Use(registry))
        using (UsageScope.Begin(session: "sess_parent", turn: 1))
        {
            await tools.StartSubSession(["web"], "Wat kan ek vanaand kook?", agent: "coder", personality: "puls-cook");
            result = await tools.ContinueSubSession("sub-1", "Maak dit vir ses mense.");
        }

        Assert.That(result, Does.StartWith("Fixed the null check; 14/14 tests pass."));
        Assert.That(result, Does.Contain("[Sub-session sub-1 on coder (web; puls-cook): 3 requests").And.Contain("ContinueSubSession(\"sub-1\""));
        Assert.That(Calls(handler), Is.EqualTo(new[] { "POST /sessions", "POST /sessions/sess_child/messages", "POST /sessions/sess_child/messages" }));
        using var followUp = JsonDocument.Parse(handler.Requests[2].Body!);
        Assert.That(followUp.RootElement.GetProperty("message").GetString(), Is.EqualTo("Maak dit vir ses mense."));
        Assert.That(registry.Open.Single().Handle, Is.EqualTo("sub-1"), "still open");
        Assert.That(sink.Records.Select(r => r.Purpose), Is.EqualTo(new[] { "sub_session", "sub_session" }), "two roll-ups, one per reply");
    }

    [Test]
    public async Task AnUnknownHandle_IsRefused_WithTheOpenOnes_WithoutANetworkCall()
    {
        var handler = Peer();
        var registry = new SubSessionRegistry();
        var tools = Tools(handler);

        string result;
        using (SubSessionRegistry.Use(registry))
        {
            await tools.StartSubSession(["web"], "x", agent: "coder", personality: "puls-cook");
            int before = handler.Requests.Count;
            result = await tools.ContinueSubSession("sub-7", "y");
            Assert.That(handler.Requests, Has.Count.EqualTo(before));
        }

        Assert.That(result, Does.StartWith("Error: no open sub-session 'sub-7'").And.Contain("sub-1 (coder: web; puls-cook)"));
    }

    [Test]
    public async Task WithoutAChat_AContinue_SaysNothingIsKept()
    {
        var handler = Peer();

        string result = await Tools(handler).ContinueSubSession("sub-1", "y");

        Assert.That(result, Does.StartWith("Error").And.Contain("keeps no sub-session open"));
        Assert.That(handler.Requests, Is.Empty);
    }

    [Test]
    public async Task AContinueThePeerNoLongerKnows_IsForgotten_AndSaysToStartOver()
    {
        // F26-S3: the peer dropped it as idle (404). Nothing to close; the model starts a new one with the whole context.
        int messages = 0;
        var handler = new StubHandler(request => request.Method.Method switch
        {
            "POST" when request.RequestUri!.AbsolutePath == "/sessions" => Json(HttpStatusCode.Created, """{"session_id":"sess_child"}"""),
            "POST" when ++messages == 1 => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Stream, Encoding.UTF8, "text/event-stream") },
            "POST" => Json(HttpStatusCode.NotFound, """{"type":"error","error":{"type":"not_found_error","message":"No session 'sess_child'"}}"""),
            _ => new HttpResponseMessage(HttpStatusCode.OK),
        });
        var registry = new SubSessionRegistry();
        var tools = Tools(handler);

        string result;
        using (SubSessionRegistry.Use(registry))
        {
            await tools.StartSubSession(["web"], "x", agent: "coder");
            result = await tools.ContinueSubSession("sub-1", "y");
        }

        Assert.That(result, Does.StartWith("Error: sub-session sub-1 on coder is no longer open").And.Contain("StartSubSession"));
        Assert.That(registry.Open, Is.Empty);
        Assert.That(handler.Requests.Any(r => r.Method == HttpMethod.Delete), Is.False, "nothing left to close");
    }

    [Test]
    public async Task AContinueThatFails_ClosesTheSubSession_SoTheNextOneStartsClean()
    {
        // A failed or timed-out follow-up leaves the peer's turn in an unknown state: close it rather than continue it.
        var handler = Peer(Replies(Stream, FailedStream));
        var registry = new SubSessionRegistry();
        var tools = Tools(handler);

        string result;
        using (SubSessionRegistry.Use(registry))
        {
            await tools.StartSubSession(["web"], "x", agent: "coder");
            result = await tools.ContinueSubSession("sub-1", "y");
        }

        Assert.That(result, Does.StartWith("Error: the sub-session on coder failed: the model refused"));
        Assert.That(handler.Requests.Last().Method, Is.EqualTo(HttpMethod.Delete));
        Assert.That(registry.Open, Is.Empty);
    }

    [Test]
    public async Task ContinuesCount_AgainstTheTurnsCalls()
    {
        // The limit is calls, not starts: a turn can't loop on one specialist either.
        var registry = new SubSessionRegistry();
        var tools = Tools(Peer());

        using (SubSessionRegistry.Use(registry))
        using (UsageScope.Begin(session: "sess_parent", turn: 1))
        {
            Assert.That(await tools.StartSubSession(["web"], "x", agent: "coder"), Does.Not.StartWith("Error"));
            Assert.That(await tools.ContinueSubSession("sub-1", "y"), Does.Not.StartWith("Error"));
            Assert.That(await tools.ContinueSubSession("sub-1", "z"), Does.Not.StartWith("Error"));
            Assert.That(await tools.ContinueSubSession("sub-1", "w"), Does.StartWith("Error").And.Contain("already made 3 sub-session calls"));
        }
    }

    [Test]
    public async Task AContinuesNamedAttachments_AreSent()
    {
        var handler = Peer();
        var registry = new SubSessionRegistry();
        var tools = Tools(handler);
        var attached = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["lys.txt"] = "uie, wortels" };

        using (SubSessionRegistry.Use(registry))
        using (SessionAttachments.Use(attached))
        {
            await tools.StartSubSession(["web"], "x", agent: "coder");
            await tools.ContinueSubSession("sub-1", "Hier is my lys.", attachments: ["lys.txt"]);
        }

        using var message = JsonDocument.Parse(handler.Requests[2].Body!);
        Assert.That(message.RootElement.GetProperty("attachments")[0].GetProperty("content").GetString(), Is.EqualTo("uie, wortels"));
    }

    /// <summary>A reply with usage and no text: what the delegation check's specialist sent after its tenth tool round.</summary>
    private const string EmptyStream = """
        event: message_delta
        data: {"type":"message_delta","delta":{"stop_reason":"end_turn"},"usage":{"input_tokens":100,"cache_creation_input_tokens":0,"cache_read_input_tokens":0,"output_tokens":104,"requests":11,"cost":0.0081,"currency":"USD"}}

        event: message_stop
        data: {"type":"message_stop"}

        """;

    private static Func<HttpResponseMessage> Replies(params string[] streams)
    {
        int next = 0;
        return () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(streams[next++], Encoding.UTF8, "text/event-stream") };
    }

    [Test]
    public async Task AnEmptyAnswer_IsAskedForOnce_InTheSameSession()
    {
        // FX11: the specialist used its ten tool rounds and ended with no text; Puls sent the whole task again, to a new one.
        var handler = Peer(Replies(EmptyStream, Stream));

        string result = await Tools(handler).StartSubSession(["files", "python"], "Count the test methods.", agent: "coder");

        Assert.That(result, Does.StartWith("Fixed the null check; 14/14 tests pass."));
        Assert.That(result, Does.Contain("11 requests, 204 tokens, 0.0081 USD + 3 requests, 10,200 tokens, 0.0123 USD"), "both turns' usage");
        Assert.That(handler.Requests.Select(r => $"{r.Method} {new Uri(r.Url).AbsolutePath}"), Is.EqualTo(new[]
        {
            "POST /sessions", "POST /sessions/sess_child/messages", "POST /sessions/sess_child/messages", "DELETE /sessions/sess_child",
        }));
        using var followUp = JsonDocument.Parse(handler.Requests[2].Body!);
        Assert.That(followUp.RootElement.GetProperty("message").GetString(), Does.Contain("answer the task now from what you found"));
    }

    [Test]
    public async Task EmptyTwice_SaysSo_AndDoesNotAskAgain()
    {
        var handler = Peer(Replies(EmptyStream, EmptyStream));

        string result = await Tools(handler).StartSubSession(["files"], "x", agent: "coder");

        Assert.That(result, Does.StartWith("Error: the sub-session on coder ended without an answer twice")
            .And.Contain("Don't send the same task again"));
        Assert.That(handler.Requests.Count(r => r.Url.EndsWith("/messages")), Is.EqualTo(2), "one follow-up, no more");
        Assert.That(handler.Requests[^1].Method, Is.EqualTo(HttpMethod.Delete));
    }

    [Test]
    public async Task NamedAttachments_AreSentWithTheTask()
    {
        // F22: the model names the file; its content comes from the session, not through the model's output.
        var handler = Peer();
        var attached = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["verslag.txt"] = "Inkomste: R1 200 000", ["other.txt"] = "x" };

        using (SessionAttachments.Use(attached))
            await Tools(handler).StartSubSession(["files"], "Som die verslag op.", agent: "coder", attachments: ["verslag.txt"]);

        using var message = JsonDocument.Parse(handler.Requests[1].Body!);
        var sent = message.RootElement.GetProperty("attachments").EnumerateArray().ToList();
        Assert.That(sent, Has.Count.EqualTo(1), "only the one named");
        Assert.That(sent[0].GetProperty("name").GetString(), Is.EqualTo("verslag.txt"));
        Assert.That(sent[0].GetProperty("content").GetString(), Is.EqualTo("Inkomste: R1 200 000"));
    }

    [Test]
    public async Task AnUnknownAttachment_IsRefused_WithoutANetworkCall()
    {
        var handler = Peer();

        string result;
        using (SessionAttachments.Use(new Dictionary<string, string> { ["verslag.txt"] = "x" }))
            result = await Tools(handler).StartSubSession(["files"], "Lees dit.", agent: "coder", attachments: ["begroting.xlsx"]);

        Assert.That(result, Does.Contain("No attachment named 'begroting.xlsx'").And.Contain("verslag.txt"));
        Assert.That(handler.Requests, Is.Empty);
    }

    [Test]
    public async Task NoAttachments_SendsAnEmptyList()
    {
        var handler = Peer();

        await Tools(handler).StartSubSession(["files"], "Fix order 1006.", agent: "coder");

        using var message = JsonDocument.Parse(handler.Requests[1].Body!);
        Assert.That(message.RootElement.GetProperty("attachments").GetArrayLength(), Is.Zero);
    }

    [Test]
    public async Task TheUsersPlace_GoesToTheSpecialist()
    {
        // F22 step 2b: the specialist's GetToday and "near me" are the user's.
        var handler = Peer();

        using (UserPlace.Use(new UserPlace("Pretoria, Gauteng, Suid-Afrika", "Africa/Johannesburg")))
            await Tools(handler).StartSubSession(["web"], "Watter restaurante is naby?", agent: "coder");

        using var create = JsonDocument.Parse(handler.Requests[0].Body!);
        Assert.That(create.RootElement.GetProperty("location").GetString(), Is.EqualTo("Pretoria, Gauteng, Suid-Afrika"));
        Assert.That(create.RootElement.GetProperty("time_zone").GetString(), Is.EqualTo("Africa/Johannesburg"));
    }

    [Test]
    public async Task APersonality_IsSentToThePeer()
    {
        // F21: the specialist's role (its function) is the personality its session opens with.
        var handler = Peer();

        await Tools(handler).StartSubSession(["web"], "What's new in .NET 10?", agent: "coder", personality: "puls-researcher");

        using var create = JsonDocument.Parse(handler.Requests[0].Body!);
        Assert.That(create.RootElement.GetProperty("personality").GetString(), Is.EqualTo("puls-researcher"));
    }

    [Test]
    public async Task EverySubSessionEvent_GoesToTheTurnsObserver_InOrder_AndTheModelStillGetsOnlyTheAnswer()
    {
        // F5: a host shows the specialist's work live; the model's tool result doesn't change.
        var handler = Peer();
        var seen = new List<SubSessionEvent>();

        string result;
        using (SubSessionFeed.Observe(seen.Add))
            result = await Tools(handler).StartSubSession(["files"], "x", agent: "coder");

        Assert.That(seen.Select(e => e.Type), Is.EqualTo(new[]
            { "message_start", "content_block_delta", "content_block_delta", "message_delta", "message_stop" }));
        Assert.That(seen.All(e => e.Agent == "coder"), Is.True);
        Assert.That(seen[1].Data, Does.Contain("Fixed the null check"));
        Assert.That(result, Does.StartWith("Fixed the null check; 14/14 tests pass.").And.Not.Contain("content_block_delta"));
    }

    [Test]
    public async Task WithoutAnObserver_NothingIsReported()
    {
        // The default: a host that doesn't observe (the CLI) is unaffected.
        Assert.That(await Tools(Peer()).StartSubSession(["files"], "x", agent: "coder"), Does.StartWith("Fixed"));
    }

    [Test]
    public async Task AnEmptyName_UsesTheOnlyPeer()
    {
        // The model used to guess an example name ("coder") first; with one peer it needn't name it at all.
        var handler = Peer();

        string result = await Tools(handler).StartSubSession(["files"], "x", agent: "");

        Assert.That(result, Does.StartWith("Fixed the null check").And.Contain("[Sub-session on coder (files)"));
        Assert.That(handler.Requests[0].Url, Does.StartWith("http://coder.test/"));
    }

    [Test]
    public async Task ACallWithoutAgent_UsesTheOnlyPeer()
    {
        // The delegation check: Puls left `agent` out and the tool loop refused the call ("missing a value for the
        // required parameter 'agent'"). Called as the model calls it, through its AIFunction, with no agent at all.
        var handler = Peer();
        var tool = AIFunctionFactory.Create(Tools(handler).StartSubSession);

        object? result = await tool.InvokeAsync(new AIFunctionArguments
        {
            ["toolsets"] = new[] { "files" }, ["message"] = "x",
        });

        Assert.That(result?.ToString(), Does.Contain("Fixed the null check").And.Contain("[Sub-session on coder (files)"));
        Assert.That(tool.JsonSchema.GetProperty("required").EnumerateArray().Select(e => e.GetString()),
            Is.EquivalentTo(new[] { "toolsets", "message" }));
    }

    [Test]
    public async Task AnEmptyName_WithSeveralPeers_AsksForOne_WithoutANetworkCall()
    {
        var handler = Peer();
        var peers = new Dictionary<string, Uri> { ["coder"] = new("http://coder.test/"), ["writer"] = new("http://writer.test/") };

        string result = await new AgentPeerTools(peers, null, new HttpClient(handler)).StartSubSession(["files"], "x", agent: " ");

        Assert.That(result, Does.Contain("more than one peer").And.Contain("coder, writer"));
        Assert.That(handler.Requests, Is.Empty);
    }

    [Test]
    public async Task AnUnknownPeer_FailsWithoutANetworkCall()
    {
        var handler = Peer();

        string result = await Tools(handler).StartSubSession(["files"], "x", agent: "nobody");

        Assert.That(result, Does.StartWith("Error: no peer agent named 'nobody'").And.Contain("coder"));
        Assert.That(handler.Requests, Is.Empty);
    }

    [Test]
    public async Task AFailedMessage_StillDeletesTheChild()
    {
        var handler = Peer(() => Json(HttpStatusCode.InternalServerError, """{"type":"error","error":{"type":"api_error","message":"boom"}}"""));

        string result = await Tools(handler).StartSubSession(["files"], "x", agent: "coder");

        Assert.That(result, Does.Contain("refused the sub-session (HTTP 500): boom"));
        Assert.That(handler.Requests.Last().Method, Is.EqualTo(HttpMethod.Delete));
    }

    [Test]
    public async Task ARefusedSession_ReportsThePeersReason()
    {
        var handler = new StubHandler(_ => Json(HttpStatusCode.BadRequest,
            """{"type":"error","error":{"type":"invalid_request_error","message":"A sub-session can't load the 'specialists' toolset"}}"""));

        string result = await Tools(handler).StartSubSession(["specialists"], "x", agent: "coder");

        Assert.That(result, Does.Contain("HTTP 400").And.Contain("can't load the 'specialists' toolset"));
        Assert.That(handler.Requests, Has.Count.EqualTo(1), "nothing was created, so nothing to delete");
    }

    [Test]
    public async Task OutsideATurn_TheLimitIsPerMinute_NotForever()
    {
        // PR #27 review: MCP calls have no turn; they used to share one key that, once full, refused every later call.
        var time = new Microsoft.Extensions.Time.Testing.FakeTimeProvider(DateTimeOffset.Parse("2026-10-13T10:00:00Z"));
        var tools = Tools(Peer(), time: time);

        for (int i = 0; i < AgentPeerTools.MaxPerTurn; i++)
            Assert.That(await tools.StartSubSession(["files"], "x", agent: "coder"), Does.Not.StartWith("Error"));
        Assert.That(await tools.StartSubSession(["files"], "x", agent: "coder"), Does.Contain("in the last minute"));

        time.Advance(AgentPeerTools.NoTurnWindow);
        Assert.That(await tools.StartSubSession(["files"], "x", agent: "coder"), Does.Not.StartWith("Error"), "a minute later it works again");
    }

    [Test]
    public async Task AnUnreadableReply_IsAnAnswer_NotACrash()
    {
        // PR #27 review: a peer replying with something other than its API's JSON used to throw past the tool.
        var handler = new StubHandler(request => request.Method.Method switch
        {
            "POST" => new HttpResponseMessage(HttpStatusCode.Created) { Content = new StringContent("<html>proxy error</html>") },
            _ => new HttpResponseMessage(HttpStatusCode.OK),
        });

        string result = await Tools(handler).StartSubSession(["files"], "x", agent: "coder");

        Assert.That(result, Does.StartWith("Error: coder's reply couldn't be read"));
    }

    [Test]
    public async Task TheFourthCallInATurn_IsRefused()
    {
        var handler = Peer();
        var tools = Tools(handler);

        using (UsageScope.Begin(session: "sess_parent", turn: 2))
        {
            for (int i = 0; i < AgentPeerTools.MaxPerTurn; i++)
                Assert.That(await tools.StartSubSession(["files"], "x", agent: "coder"), Does.Not.StartWith("Error"));

            Assert.That(await tools.StartSubSession(["files"], "x", agent: "coder"), Does.Contain("already made 3 sub-session calls"));
        }

        using (UsageScope.Begin(session: "sess_parent", turn: 3))
            Assert.That(await tools.StartSubSession(["files"], "x", agent: "coder"), Does.Not.StartWith("Error"), "a new turn starts again");
    }

    [Test]
    public async Task ATimeout_ReturnsAnErrorTheParentCanHandle_AndDeletesTheChild()
    {
        var handler = new SlowPeer();

        string result = await new AgentPeerTools(Peers, null, new HttpClient(handler), TimeSpan.FromMilliseconds(200))
            .StartSubSession(["files"], "x", agent: "coder");

        Assert.That(result, Does.Contain("didn't finish within"));
        Assert.That(handler.Deleted, Is.True);
    }

    [Test]
    public async Task TheParentTurn_IsSentSoTheChildsUsageLinksToIt()
    {
        // Step 9: the child's records carry parent = "{session}#{turn}" of the turn that opened it.
        var handler = Peer();

        using (UsageScope.Begin(session: "sess_parent", turn: 4, tags: new UsageTags(null, "run-1", "order-service")))
            await Tools(handler).StartSubSession(["files"], "x", agent: "coder");

        using var create = JsonDocument.Parse(handler.Requests[0].Body!);
        Assert.That(create.RootElement.GetProperty("parent").GetString(), Is.EqualTo("sess_parent#4"));
        Assert.That(create.RootElement.GetProperty("run").GetString(), Is.EqualTo("run-1"));
    }

    [Test]
    public void Peers_AreParsedFromAgentPeers()
    {
        var peers = AgentOptions.ParsePeers("coder=http://coder:8080, Writer = https://w.example/api ,bad,=http://x,ftp=ftp://x");

        Assert.That(peers.Keys, Is.EquivalentTo(new[] { "coder", "writer" }));
        Assert.That(peers["coder"].ToString(), Is.EqualTo("http://coder:8080/"));
        Assert.That(peers["writer"].ToString(), Is.EqualTo("https://w.example/api/"));
    }

    /// <summary>Creates the child, then never answers the message until cancelled; records the delete.</summary>
    private sealed class SlowPeer : HttpMessageHandler
    {
        public bool Deleted { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Delete) { Deleted = true; return new HttpResponseMessage(HttpStatusCode.OK); }
            if (request.RequestUri!.AbsolutePath == "/sessions") return Json(HttpStatusCode.Created, """{"session_id":"sess_child"}""");
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
