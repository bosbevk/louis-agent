using System.Net;
using System.Text;
using System.Text.Json;
using LouisAgent.Core.Config;
using LouisAgent.Core.Tools.Agents;
using LouisAgent.Core.Usage;

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

    private static AgentPeerTools Tools(StubHandler handler, TimeSpan? timeout = null) =>
        new(Peers, "the-key", new HttpClient(handler), timeout);

    [Test]
    public async Task ASubSession_ReturnsTheAnswerAndUsage_AndIsDeleted()
    {
        var handler = Peer();

        string result = await Tools(handler).StartSubSession("coder", ["files", "git"], "Fix order 1006.");

        Assert.That(result, Does.StartWith("Fixed the null check; 14/14 tests pass."));
        Assert.That(result, Does.Contain("[Sub-session on coder (files, git): 3 requests, 10,200 tokens, 0.0123 USD]"));
        Assert.That(handler.Requests.Select(r => $"{r.Method} {new Uri(r.Url).AbsolutePath}"),
            Is.EqualTo(new[] { "POST /sessions", "POST /sessions/sess_child/messages", "DELETE /sessions/sess_child" }));
        using var create = JsonDocument.Parse(handler.Requests[0].Body!);
        Assert.That(create.RootElement.GetProperty("depth").GetInt32(), Is.EqualTo(1), "one level deep");
        Assert.That(create.RootElement.GetProperty("toolsets").EnumerateArray().Select(t => t.GetString()), Is.EqualTo(new[] { "files", "git" }));
        Assert.That(create.RootElement.GetProperty("personality").ValueKind, Is.EqualTo(JsonValueKind.Null), "none named: the peer's default");
    }

    [Test]
    public async Task NamedAttachments_AreSentWithTheTask()
    {
        // F22: the model names the file; its content comes from the session, not through the model's output.
        var handler = Peer();
        var attached = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["verslag.txt"] = "Inkomste: R1 200 000", ["other.txt"] = "x" };

        using (SessionAttachments.Use(attached))
            await Tools(handler).StartSubSession("coder", ["files"], "Som die verslag op.", attachments: ["verslag.txt"]);

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
            result = await Tools(handler).StartSubSession("coder", ["files"], "Lees dit.", attachments: ["begroting.xlsx"]);

        Assert.That(result, Does.Contain("No attachment named 'begroting.xlsx'").And.Contain("verslag.txt"));
        Assert.That(handler.Requests, Is.Empty);
    }

    [Test]
    public async Task NoAttachments_SendsAnEmptyList()
    {
        var handler = Peer();

        await Tools(handler).StartSubSession("coder", ["files"], "Fix order 1006.");

        using var message = JsonDocument.Parse(handler.Requests[1].Body!);
        Assert.That(message.RootElement.GetProperty("attachments").GetArrayLength(), Is.Zero);
    }

    [Test]
    public async Task APersonality_IsSentToThePeer()
    {
        // F21: the specialist's role (its function) is the personality its session opens with.
        var handler = Peer();

        await Tools(handler).StartSubSession("coder", ["web"], "What's new in .NET 10?", personality: "puls-researcher");

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
            result = await Tools(handler).StartSubSession("coder", ["files"], "x");

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
        Assert.That(await Tools(Peer()).StartSubSession("coder", ["files"], "x"), Does.StartWith("Fixed"));
    }

    [Test]
    public async Task AnEmptyName_UsesTheOnlyPeer()
    {
        // The model used to guess an example name ("coder") first; with one peer it needn't name it at all.
        var handler = Peer();

        string result = await Tools(handler).StartSubSession("", ["files"], "x");

        Assert.That(result, Does.StartWith("Fixed the null check").And.Contain("[Sub-session on coder (files)"));
        Assert.That(handler.Requests[0].Url, Does.StartWith("http://coder.test/"));
    }

    [Test]
    public async Task AnEmptyName_WithSeveralPeers_AsksForOne_WithoutANetworkCall()
    {
        var handler = Peer();
        var peers = new Dictionary<string, Uri> { ["coder"] = new("http://coder.test/"), ["writer"] = new("http://writer.test/") };

        string result = await new AgentPeerTools(peers, null, new HttpClient(handler)).StartSubSession(" ", ["files"], "x");

        Assert.That(result, Does.Contain("more than one peer").And.Contain("coder, writer"));
        Assert.That(handler.Requests, Is.Empty);
    }

    [Test]
    public async Task AnUnknownPeer_FailsWithoutANetworkCall()
    {
        var handler = Peer();

        string result = await Tools(handler).StartSubSession("nobody", ["files"], "x");

        Assert.That(result, Does.StartWith("Error: no peer agent named 'nobody'").And.Contain("coder"));
        Assert.That(handler.Requests, Is.Empty);
    }

    [Test]
    public async Task AFailedMessage_StillDeletesTheChild()
    {
        var handler = Peer(() => Json(HttpStatusCode.InternalServerError, """{"type":"error","error":{"type":"api_error","message":"boom"}}"""));

        string result = await Tools(handler).StartSubSession("coder", ["files"], "x");

        Assert.That(result, Does.Contain("refused the sub-session (HTTP 500): boom"));
        Assert.That(handler.Requests.Last().Method, Is.EqualTo(HttpMethod.Delete));
    }

    [Test]
    public async Task ARefusedSession_ReportsThePeersReason()
    {
        var handler = new StubHandler(_ => Json(HttpStatusCode.BadRequest,
            """{"type":"error","error":{"type":"invalid_request_error","message":"A sub-session can't load the 'agents' toolset"}}"""));

        string result = await Tools(handler).StartSubSession("coder", ["agents"], "x");

        Assert.That(result, Does.Contain("HTTP 400").And.Contain("can't load the 'agents' toolset"));
        Assert.That(handler.Requests, Has.Count.EqualTo(1), "nothing was created, so nothing to delete");
    }

    [Test]
    public async Task TheFourthCallInATurn_IsRefused()
    {
        var handler = Peer();
        var tools = Tools(handler);

        using (UsageScope.Begin(session: "sess_parent", turn: 2))
        {
            for (int i = 0; i < AgentPeerTools.MaxPerTurn; i++)
                Assert.That(await tools.StartSubSession("coder", ["files"], "x"), Does.Not.StartWith("Error"));

            Assert.That(await tools.StartSubSession("coder", ["files"], "x"), Does.Contain("already started 3 sub-sessions"));
        }

        using (UsageScope.Begin(session: "sess_parent", turn: 3))
            Assert.That(await tools.StartSubSession("coder", ["files"], "x"), Does.Not.StartWith("Error"), "a new turn starts again");
    }

    [Test]
    public async Task ATimeout_ReturnsAnErrorTheParentCanHandle_AndDeletesTheChild()
    {
        var handler = new SlowPeer();

        string result = await new AgentPeerTools(Peers, null, new HttpClient(handler), TimeSpan.FromMilliseconds(200))
            .StartSubSession("coder", ["files"], "x");

        Assert.That(result, Does.Contain("didn't finish within"));
        Assert.That(handler.Deleted, Is.True);
    }

    [Test]
    public async Task TheParentTurn_IsSentSoTheChildsUsageLinksToIt()
    {
        // Step 9: the child's records carry parent = "{session}#{turn}" of the turn that opened it.
        var handler = Peer();

        using (UsageScope.Begin(session: "sess_parent", turn: 4, tags: new UsageTags(null, "run-1", "order-service")))
            await Tools(handler).StartSubSession("coder", ["files"], "x");

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
