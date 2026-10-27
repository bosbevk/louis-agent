using LouisAgent.Testing;
using LouisAgent.Choices;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Sessions.SubSessions;
using LouisAgent.Core.Usage.Records;
using LouisAgent.Core.Usage.Sinks;
using LouisAgent.Core.Usage.Reports;
using System.Net;
using System.Text;
using System.Text.Json;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Sessions;
using LouisAgent.Core.Sessions.Replay;
using LouisAgent.Core.Config;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Providers;
using LouisAgent.Core.Tools.Agents;
using LouisAgent.Core.Tools.Clock;
using Microsoft.Extensions.AI;

using static LouisAgent.Core.Tests.Tools.Agents.FakePeer;

namespace LouisAgent.Core.Tests.Tools.Agents;

// F5 step 8 (F5-S5): a sub-session on a peer, with its limits in code. The peer is a stub (FakePeer): no network.
public class SpecialistToolsTests
{
    [Test]
    public async Task ASubSession_ReturnsTheAnswerAndUsage_AndIsDeleted()
    {
        var handler = Peer();

        string result = await ToolsOn(handler).StartSubSession([CoreToolset.Files.Wire(), CoreToolset.Git.Wire()], "Fix order 1006.", agent: "coder");

        Assert.That(result, Does.StartWith("Fixed the null check; 14/14 tests pass."));
        Assert.That(result, Does.Contain("[Sub-session on coder (files, git): 3 requests, 10,200 tokens, 0.0123 USD]"));
        Assert.That(handler.Requests.Select(r => $"{r.Method} {new Uri(r.Url).AbsolutePath}"),
            Is.EqualTo(new[] { "POST /v1/sessions", "POST /v1/sessions/sess_child/messages", "DELETE /v1/sessions/sess_child" }));
        using var create = JsonDocument.Parse(handler.Requests[0].Body!);
        Assert.That(create.RootElement.GetProperty("depth").GetInt32(), Is.EqualTo(1), "one level deep");
        Assert.That(create.RootElement.GetProperty("toolsets").EnumerateArray().Select(t => t.GetString()), Is.EqualTo(new[] { CoreToolset.Files.Wire(), CoreToolset.Git.Wire() }));
        Assert.That(create.RootElement.GetProperty("role").ValueKind, Is.EqualTo(JsonValueKind.Null), "none named: the peer's default");
        Assert.That(create.RootElement.GetProperty("llm").ValueKind, Is.EqualTo(JsonValueKind.Null), "none named: the peer's own model");
    }

    // F34, Louis's screenshot: the cook's card showed `web` only, though its role adds `shops` on the peer. The peer
    // says what the session loaded; the footer and the kept sub-session say that, not just what was sent.
    [Test]
    public async Task TheFooter_NamesTheToolsetsThePeerLoaded()
    {
        var handler = new StubHandler(request => request.Method.Method switch
        {
            "POST" when request.RequestUri!.AbsolutePath == "/v1/sessions" =>
                Json(HttpStatusCode.Created, SampleToolset.Fill("""{"session_id":"sess_child","toolsets":["<Alpha>","<Beta>"],"role":"puls-cook"}""")),
            "POST" => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(AnswerStream, Encoding.UTF8, "text/event-stream") },
            _ => new HttpResponseMessage(HttpStatusCode.NotFound),
        });
        var registry = new SubSessionRegistry();

        string result;
        using (SubSessionRegistry.Use(registry))
            result = await ToolsOn(handler).StartSubSession([CoreToolset.Web.Wire()], "Wat kook ek?", agent: "coder", role: "puls-cook");

        Assert.That(result, Does.Contain(SampleToolset.Fill("[Sub-session sub-1 on coder (<Alpha>, <Beta>; puls-cook)")));
        Assert.That(registry.Open.Single().Toolsets, Is.EqualTo(new[] { SampleToolset.Alpha, SampleToolset.Beta }));
    }

    [Test]
    public async Task AnEmptyAnswer_IsAskedForOnce_InTheSameSession()
    {
        // FX11: the specialist used its ten tool rounds and ended with no text; Puls sent the whole task again, to a new one.
        var handler = Peer(Replies(EmptyStream, AnswerStream));

        string result = await ToolsOn(handler).StartSubSession([CoreToolset.Files.Wire(), CoreToolset.Python.Wire()], "Count the test methods.", agent: "coder");

        Assert.That(result, Does.StartWith("Fixed the null check; 14/14 tests pass."));
        Assert.That(result, Does.Contain("11 requests, 204 tokens, 0.0081 USD + 3 requests, 10,200 tokens, 0.0123 USD"), "both turns' usage");
        Assert.That(handler.Requests.Select(r => $"{r.Method} {new Uri(r.Url).AbsolutePath}"), Is.EqualTo(new[]
        {
            "POST /v1/sessions", "POST /v1/sessions/sess_child/messages", "POST /v1/sessions/sess_child/messages", "DELETE /v1/sessions/sess_child",
        }));
        using var followUp = JsonDocument.Parse(handler.Requests[2].Body!);
        Assert.That(followUp.RootElement.GetProperty("message").GetString(), Does.Contain("answer the task now from what you found"));
    }

    [Test]
    public async Task EmptyTwice_SaysSo_AndDoesNotAskAgain()
    {
        var handler = Peer(Replies(EmptyStream, EmptyStream));

        string result = await ToolsOn(handler).StartSubSession([CoreToolset.Files.Wire()], "x", agent: "coder");

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
            await ToolsOn(handler).StartSubSession([CoreToolset.Files.Wire()], "Som die verslag op.", agent: "coder", attachments: ["verslag.txt"]);

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
            result = await ToolsOn(handler).StartSubSession([CoreToolset.Files.Wire()], "Lees dit.", agent: "coder", attachments: ["begroting.xlsx"]);

        Assert.That(result, Does.Contain("No attachment named 'begroting.xlsx'").And.Contain("verslag.txt"));
        Assert.That(handler.Requests, Is.Empty);
    }

    [Test]
    public async Task NoAttachments_SendsAnEmptyList()
    {
        var handler = Peer();

        await ToolsOn(handler).StartSubSession([CoreToolset.Files.Wire()], "Fix order 1006.", agent: "coder");

        using var message = JsonDocument.Parse(handler.Requests[1].Body!);
        Assert.That(message.RootElement.GetProperty("attachments").GetArrayLength(), Is.Zero);
    }

    [Test]
    public async Task TheUsersPlace_GoesToTheSpecialist()
    {
        // F22 step 2b: the specialist's GetToday and "near me" are the user's.
        var handler = Peer();

        using (UserPlace.Use(new UserPlace("Pretoria, Gauteng, Suid-Afrika", "Africa/Johannesburg")))
            await ToolsOn(handler).StartSubSession([CoreToolset.Web.Wire()], "Watter restaurante is naby?", agent: "coder");

        using var create = JsonDocument.Parse(handler.Requests[0].Body!);
        Assert.That(create.RootElement.GetProperty("location").GetString(), Is.EqualTo("Pretoria, Gauteng, Suid-Afrika"));
        Assert.That(create.RootElement.GetProperty("time_zone").GetString(), Is.EqualTo("Africa/Johannesburg"));
    }

    [TestCase(WebReplayMode.Record, "record")]
    [TestCase(WebReplayMode.Replay, "replay")]
    public async Task TheReplay_GoesToTheSpecialist_AsTheParentsOwnTrial(WebReplayMode mode, string expected)
    {
        // F18 step 4a: the child never works out a key of its own (its task is sub-session:{name}, and one run tag holds every scenario).
        var handler = Peer();

        using (SessionReplay.Use(new SessionReplay(mode, "run_1:puls-cook:1/bobotie", Model: false)))
            await ToolsOn(handler).StartSubSession([CoreToolset.Web.Wire()], "Maak 'n bobotie.", agent: "coder");

        using var create = JsonDocument.Parse(handler.Requests[0].Body!);
        var replay = create.RootElement.GetProperty("replay");
        Assert.That(replay.GetProperty("web").GetString(), Is.EqualTo(expected));
        Assert.That(replay.GetProperty("of").GetString(), Is.EqualTo("run_1:puls-cook:1/bobotie"));
        Assert.That(replay.GetProperty("model").GetBoolean(), Is.False);
    }

    [Test]
    public async Task WithoutAReplay_TheSpecialistIsToldNone()
    {
        var handler = Peer();

        await ToolsOn(handler).StartSubSession([CoreToolset.Web.Wire()], "Maak 'n bobotie.", agent: "coder");

        using var create = JsonDocument.Parse(handler.Requests[0].Body!);
        Assert.That(create.RootElement.TryGetProperty("replay", out var replay) && replay.ValueKind == JsonValueKind.Object, Is.False);
    }

    [Test]
    public async Task ARole_IsSentToThePeer()
    {
        // F21: the specialist's role (its function) is the role its session opens with (CL14: the body says role).
        var handler = Peer();

        await ToolsOn(handler).StartSubSession([CoreToolset.Web.Wire()], "What's new in .NET 10?", agent: "coder", role: "puls-researcher");

        using var create = JsonDocument.Parse(handler.Requests[0].Body!);
        Assert.That(create.RootElement.GetProperty("role").GetString(), Is.EqualTo("puls-researcher"));
    }

    [Test]
    public async Task AnLlmProfile_IsSentToThePeer_Trimmed()
    {
        // F30: the price lookup names the peer's local model; the peer refuses a profile it doesn't offer.
        var handler = Peer();

        await ToolsOn(handler).StartSubSession([CoreToolset.Web.Wire()], "Prys van 1 kg uie in Stellenbosch?", agent: "coder", role: "puls-prices",
            llm: " ollama-puls ");

        using var create = JsonDocument.Parse(handler.Requests[0].Body!);
        Assert.That(create.RootElement.GetProperty("llm").GetString(), Is.EqualTo("ollama-puls"));
    }

    [Test]
    public async Task EverySubSessionEvent_GoesToTheTurnsObserver_InOrder_AndTheModelStillGetsOnlyTheAnswer()
    {
        // F5: a host shows the specialist's work live; the model's tool result doesn't change.
        var handler = Peer();
        var seen = new List<SubSessionEvent>();

        string result;
        using (SubSessionFeed.Observe(seen.Add))
            result = await ToolsOn(handler).StartSubSession([CoreToolset.Files.Wire()], "x", agent: "coder");

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
        Assert.That(await ToolsOn(Peer()).StartSubSession([CoreToolset.Files.Wire()], "x", agent: "coder"), Does.StartWith("Fixed"));
    }

    [Test]
    public async Task AnEmptyName_UsesTheOnlyPeer()
    {
        // The model used to guess an example name ("coder") first; with one peer it needn't name it at all.
        var handler = Peer();

        string result = await ToolsOn(handler).StartSubSession([CoreToolset.Files.Wire()], "x", agent: "");

        Assert.That(result, Does.StartWith("Fixed the null check").And.Contain("[Sub-session on coder (files)"));
        Assert.That(handler.Requests[0].Url, Does.StartWith("http://coder.test/"));
    }

    [Test]
    public async Task ACallWithoutAgent_UsesTheOnlyPeer()
    {
        // The delegation check: Puls left `agent` out and the tool loop refused the call ("missing a value for the
        // required parameter 'agent'"). Called as the model calls it, through its AIFunction, with no agent at all.
        var handler = Peer();
        var tool = AIFunctionFactory.Create(ToolsOn(handler).StartSubSession);

        object? result = await tool.InvokeAsync(new AIFunctionArguments
        {
            ["toolsets"] = new[] { CoreToolset.Files.Wire() }, ["message"] = "x",
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

        string result = await new SpecialistTools(peers, null, new HttpClient(handler)).StartSubSession([CoreToolset.Files.Wire()], "x", agent: " ");

        Assert.That(result, Does.Contain("more than one peer").And.Contain("coder, writer"));
        Assert.That(handler.Requests, Is.Empty);
    }

    [Test]
    public async Task AnUnknownPeer_FailsWithoutANetworkCall()
    {
        var handler = Peer();

        string result = await ToolsOn(handler).StartSubSession([CoreToolset.Files.Wire()], "x", agent: "nobody");

        Assert.That(result, Does.StartWith("Error: no peer agent named 'nobody'").And.Contain("coder"));
        Assert.That(handler.Requests, Is.Empty);
    }

    [Test]
    public async Task AFailedMessage_StillDeletesTheChild()
    {
        var handler = Peer(() => Json(HttpStatusCode.InternalServerError, """{"type":"error","error":{"type":"api_error","message":"boom"}}"""));

        string result = await ToolsOn(handler).StartSubSession([CoreToolset.Files.Wire()], "x", agent: "coder");

        Assert.That(result, Does.Contain("refused the sub-session (HTTP 500): boom"));
        Assert.That(handler.Requests.Last().Method, Is.EqualTo(HttpMethod.Delete));
    }

    [Test]
    public async Task ARefusedSession_ReportsThePeersReason()
    {
        var handler = new StubHandler(_ => Json(HttpStatusCode.BadRequest,
            """{"type":"error","error":{"type":"invalid_request_error","message":"A sub-session can't load the 'specialists' toolset"}}"""));

        string result = await ToolsOn(handler).StartSubSession([CoreToolset.Specialists.Wire()], "x", agent: "coder");

        Assert.That(result, Does.Contain("HTTP 400").And.Contain("can't load the 'specialists' toolset"));
        Assert.That(handler.Requests, Has.Count.EqualTo(1), "nothing was created, so nothing to delete");
    }

    [Test]
    public async Task OutsideATurn_TheLimitIsPerMinute_NotForever()
    {
        // PR #27 review: MCP calls have no turn; they used to share one key that, once full, refused every later call.
        var time = new Microsoft.Extensions.Time.Testing.FakeTimeProvider(DateTimeOffset.Parse("2026-10-13T10:00:00Z"));
        var tools = ToolsOn(Peer(), time: time);

        for (int i = 0; i < SpecialistTools.MaxPerTurn; i++)
            Assert.That(await tools.StartSubSession([CoreToolset.Files.Wire()], "x", agent: "coder"), Does.Not.StartWith("Error"));
        Assert.That(await tools.StartSubSession([CoreToolset.Files.Wire()], "x", agent: "coder"), Does.Contain("in the last minute"));

        time.Advance(SpecialistTools.NoTurnWindow);
        Assert.That(await tools.StartSubSession([CoreToolset.Files.Wire()], "x", agent: "coder"), Does.Not.StartWith("Error"), "a minute later it works again");
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

        string result = await ToolsOn(handler).StartSubSession([CoreToolset.Files.Wire()], "x", agent: "coder");

        Assert.That(result, Does.StartWith("Error: coder's reply couldn't be read"));
    }

    [Test]
    public async Task TheFourthCallInATurn_IsRefused()
    {
        var handler = Peer();
        var tools = ToolsOn(handler);

        using (UsageScope.Begin(session: "sess_parent", turn: 2))
        {
            for (int i = 0; i < SpecialistTools.MaxPerTurn; i++)
                Assert.That(await tools.StartSubSession([CoreToolset.Files.Wire()], "x", agent: "coder"), Does.Not.StartWith("Error"));

            Assert.That(await tools.StartSubSession([CoreToolset.Files.Wire()], "x", agent: "coder"), Does.Contain("already made 3 sub-session calls"));
        }

        using (UsageScope.Begin(session: "sess_parent", turn: 3))
            Assert.That(await tools.StartSubSession([CoreToolset.Files.Wire()], "x", agent: "coder"), Does.Not.StartWith("Error"), "a new turn starts again");
    }

    [Test]
    public async Task ATimeout_ReturnsAnErrorTheParentCanHandle_AndDeletesTheChild()
    {
        var handler = new SlowPeer();

        string result = await new SpecialistTools(Peers, null, new HttpClient(handler), TimeSpan.FromMilliseconds(200))
            .StartSubSession([CoreToolset.Files.Wire()], "x", agent: "coder");

        Assert.That(result, Does.Contain("didn't finish within"));
        Assert.That(handler.Deleted, Is.True);
    }

    [Test]
    public void Peers_AreParsedFromAgentPeers()
    {
        var peers = AgentOptions.ParsePeers("coder=http://coder:8080, Writer = https://w.example/api ,bad,=http://x,ftp=ftp://x");

        Assert.That(peers.Keys, Is.EquivalentTo(new[] { "coder", "writer" }));
        Assert.That(peers["coder"].ToString(), Is.EqualTo("http://coder:8080/"));
        Assert.That(peers["writer"].ToString(), Is.EqualTo("https://w.example/api/"));
    }
}
