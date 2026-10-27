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
using LouisAgent.Core.Config;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Providers;
using LouisAgent.Core.Tools.Agents;
using LouisAgent.Core.Tools.Clock;
using Microsoft.Extensions.AI;

using static LouisAgent.Core.Tests.Tools.Agents.FakePeer;

namespace LouisAgent.Core.Tests.Tools.Agents;

// F26: a chat keeps a sub-session for a follow-up; ContinueSubSession finds it by its handle, and what the peer dropped is forgotten.
public class SpecialistToolsKeptSessionTests
{
    [Test]
    public async Task InAChat_TheSubSessionIsKept_AndTheAnswerNamesItsHandle()
    {
        // F26 step 1: with the chat's registry current, the specialist stays open for a follow-up, and the footer tells
        // the model how to continue it. Without a registry (an MCP call), it is deleted as before.
        var handler = Peer();
        var registry = new SubSessionRegistry();

        string result;
        using (SubSessionRegistry.Use(registry))
            result = await ToolsOn(handler).StartSubSession([CoreToolset.Web.Wire()], "Wat kan ek vanaand kook?", agent: "coder", role: SampleRole.Alpha);

        Assert.That(result, Does.StartWith("Fixed the null check; 14/14 tests pass."));
        Assert.That(result, Does.Contain(SampleRole.Fill("[Sub-session sub-1 on coder (web; <RoleAlpha>): 3 requests, 10,200 tokens, 0.0123 USD"))
            .And.Contain("ContinueSubSession(\"sub-1\""));
        Assert.That(Calls(handler), Is.EqualTo(new[] { "POST /v1/sessions", "POST /v1/sessions/sess_child/messages" }), "no DELETE");
        OpenSubSession open = registry.Open.Single();
        Assert.That((open.Handle, open.SessionId, open.Agent, open.Role), Is.EqualTo(("sub-1", "sess_child", "coder", SampleRole.Alpha)));
        Assert.That(open.Toolsets, Is.EqualTo(new[] { CoreToolset.Web.Wire() }));
    }

    [Test]
    public async Task AFailedStart_IsNotKept()
    {
        var handler = Peer(Replies(FailedStream));
        var registry = new SubSessionRegistry();

        using (SubSessionRegistry.Use(registry))
            await ToolsOn(handler).StartSubSession([CoreToolset.Files.Wire()], "x", agent: "coder");

        Assert.That(handler.Requests.Last().Method, Is.EqualTo(HttpMethod.Delete));
        Assert.That(registry.Open, Is.Empty);
    }

    [Test]
    public async Task PastTheChatsCap_TheOldestKeptSubSession_IsDeletedOnThePeer()
    {
        // F26-S3: the registry evicts; the tool's closer sends the DELETE with the API key.
        var handler = PeerWithIds();
        var registry = new SubSessionRegistry(cap: 2);
        var tools = ToolsOn(handler);

        using (SubSessionRegistry.Use(registry))
        using (UsageScope.Begin(session: "sess_parent", turn: 1))
        {
            for (int i = 0; i < 3; i++)
                Assert.That(await tools.StartSubSession([CoreToolset.Files.Wire()], "x", agent: "coder"), Does.Not.StartWith("Error"));
        }

        Assert.That(Calls(handler).Where(c => c.StartsWith("DELETE")), Is.EqualTo(new[] { "DELETE /v1/sessions/sess_1" }));
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
        var tools = ToolsOn(handler, sink: sink);

        string result;
        using (SubSessionRegistry.Use(registry))
        using (UsageScope.Begin(session: "sess_parent", turn: 1))
        {
            await tools.StartSubSession([CoreToolset.Web.Wire()], "Wat kan ek vanaand kook?", agent: "coder", role: SampleRole.Alpha);
            result = await tools.ContinueSubSession("sub-1", "Maak dit vir ses mense.");
        }

        Assert.That(result, Does.StartWith("Fixed the null check; 14/14 tests pass."));
        Assert.That(result, Does.Contain(SampleRole.Fill("[Sub-session sub-1 on coder (web; <RoleAlpha>): 3 requests")).And.Contain("ContinueSubSession(\"sub-1\""));
        Assert.That(Calls(handler), Is.EqualTo(new[] { "POST /v1/sessions", "POST /v1/sessions/sess_child/messages", "POST /v1/sessions/sess_child/messages" }));
        using var followUp = JsonDocument.Parse(handler.Requests[2].Body!);
        Assert.That(followUp.RootElement.GetProperty("message").GetString(), Is.EqualTo("Maak dit vir ses mense."));
        Assert.That(registry.Open.Single().Handle, Is.EqualTo("sub-1"), "still open");
        Assert.That(sink.Records.Select(r => r.Purpose), Is.EqualTo(new[] { "sub_session", "sub_session" }), "two roll-ups, one per reply");
    }

    [Test]
    public async Task APeerThatSaysPersonality_IsNotRead_TheRolePassedIsKept()
    {
        // CL20 step 5: a peer that knows /v1 says "role"; the old word "personality" is not read, so the kept sub-session keeps the role passed.
        var handler = new StubHandler(request => request.Method.Method switch
        {
            "POST" when request.RequestUri!.AbsolutePath == "/v1/sessions" => Json(HttpStatusCode.Created, SampleRole.Fill("""{"session_id":"sess_child","personality":"<RoleEpsilon>"}""")),
            "POST" => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(AnswerStream, Encoding.UTF8, "text/event-stream") },
            "DELETE" => Json(HttpStatusCode.OK, """{"deleted":true}"""),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound),
        });
        var registry = new SubSessionRegistry();

        using (SubSessionRegistry.Use(registry))
            await ToolsOn(handler).StartSubSession([CoreToolset.Web.Wire()], "x", agent: "coder", role: SampleRole.Alpha);

        Assert.That(registry.Open.Single().Role, Is.EqualTo(SampleRole.Alpha));
    }

    [Test]
    public async Task AnUnknownHandle_IsRefused_WithTheOpenOnes_WithoutANetworkCall()
    {
        var handler = Peer();
        var registry = new SubSessionRegistry();
        var tools = ToolsOn(handler);

        string result;
        using (SubSessionRegistry.Use(registry))
        {
            await tools.StartSubSession([CoreToolset.Web.Wire()], "x", agent: "coder", role: SampleRole.Alpha);
            int before = handler.Requests.Count;
            result = await tools.ContinueSubSession("sub-7", "y");
            Assert.That(handler.Requests, Has.Count.EqualTo(before));
        }

        Assert.That(result, Does.StartWith("Error: no open sub-session 'sub-7'").And.Contain(SampleRole.Fill("sub-1 (coder: web; <RoleAlpha>)")));
    }

    [Test]
    public async Task WithoutAChat_AContinue_SaysNothingIsKept()
    {
        var handler = Peer();

        string result = await ToolsOn(handler).ContinueSubSession("sub-1", "y");

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
            "POST" when request.RequestUri!.AbsolutePath == "/v1/sessions" => Json(HttpStatusCode.Created, """{"session_id":"sess_child"}"""),
            "POST" when ++messages == 1 => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(AnswerStream, Encoding.UTF8, "text/event-stream") },
            "POST" => Json(HttpStatusCode.NotFound, """{"type":"error","error":{"type":"not_found_error","message":"No session 'sess_child'"}}"""),
            _ => new HttpResponseMessage(HttpStatusCode.OK),
        });
        var registry = new SubSessionRegistry();
        var tools = ToolsOn(handler);

        string result;
        using (SubSessionRegistry.Use(registry))
        {
            await tools.StartSubSession([CoreToolset.Web.Wire()], "x", agent: "coder");
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
        var handler = Peer(Replies(AnswerStream, FailedStream));
        var registry = new SubSessionRegistry();
        var tools = ToolsOn(handler);

        string result;
        using (SubSessionRegistry.Use(registry))
        {
            await tools.StartSubSession([CoreToolset.Web.Wire()], "x", agent: "coder");
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
        var tools = ToolsOn(Peer());

        using (SubSessionRegistry.Use(registry))
        using (UsageScope.Begin(session: "sess_parent", turn: 1))
        {
            Assert.That(await tools.StartSubSession([CoreToolset.Web.Wire()], "x", agent: "coder"), Does.Not.StartWith("Error"));
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
        var tools = ToolsOn(handler);
        var attached = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["lys.txt"] = "uie, wortels" };

        using (SubSessionRegistry.Use(registry))
        using (SessionAttachments.Use(attached))
        {
            await tools.StartSubSession([CoreToolset.Web.Wire()], "x", agent: "coder");
            await tools.ContinueSubSession("sub-1", "Hier is my lys.", attachments: ["lys.txt"]);
        }

        using var message = JsonDocument.Parse(handler.Requests[2].Body!);
        Assert.That(message.RootElement.GetProperty("attachments")[0].GetProperty("content").GetString(), Is.EqualTo("uie, wortels"));
    }
}
