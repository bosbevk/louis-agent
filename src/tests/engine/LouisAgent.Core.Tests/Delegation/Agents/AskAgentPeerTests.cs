using System.Net;
using System.Text.Json;
using LouisAgent.Composition.Packs;
using LouisAgent.Composition.Sessions;
using LouisAgent.Core.Delegation.Agents;
using LouisAgent.Core.Sessions;
using LouisAgent.Core.Sessions.Replay;
using LouisAgent.Core.Usage.Records;
using static LouisAgent.Core.Tests.Delegation.FakePeer;

namespace LouisAgent.Core.Tests.Delegation.Agents;

/// <summary>
/// The path every ask takes to its peer (F127.3.1 step 11d, from the specialist tool's tests): the answer read to the
/// end and the child deleted outside a chat, an empty answer asked for once more, what goes with the message (the
/// files, the user's place, the replay, the parent turn), and every failure an answer the model can act on.
/// </summary>
public class AskAgentPeerTests
{
    /// <summary>The asker, who may ask testprod in its voice.</summary>
    private static readonly FakeIdentity Guide = new("guide", voice: "testprod/voice-en", calls: ["testprod"]);

    /// <summary>The identities this instance runs.</summary>
    private static readonly IReadOnlyList<IAgentIdentity> Identities =
        [Guide, new FakeIdentity("testprod", voice: "testprod/voice-en")];

    /// <summary>Asks testprod as the guide, on this peer, outside a chat.</summary>
    private static async Task<string> AskAsync(HttpMessageHandler handler, string message = "Is a dog allowed?",
        string[]? attachments = null)
    {
        using var _ = TurnIdentity.Use(TurnIdentity.Of(Guide));
        return await new AskAgentTools(Peers, Identities, "the-key", new HttpClient(handler))
            .AskAgent("testprod", message, attachments);
    }

    /// <summary>Outside a chat the answer comes back and the child is deleted, since nobody can follow up.</summary>
    [Test]
    public async Task OutsideAChat_TheAnswerComesBack_AndTheChildIsDeleted()
    {
        var handler = Peer();

        string answer = await AskAsync(handler);

        Assert.Multiple(() =>
        {
            Assert.That(answer, Does.StartWith("Fixed the null check; 14/14 tests pass."));
            Assert.That(Calls(handler),
                Is.EqualTo(new[]
                {
                    "POST /v1/sessions", "POST /v1/sessions/sess_child/messages", "DELETE /v1/sessions/sess_child"
                }));
        });
    }

    /// <summary>
    /// An agent that used its tool rounds and gave no text is asked once more for its answer, in the same session
    /// (FX11).
    /// </summary>
    [Test]
    public async Task AnEmptyAnswer_IsAskedForOnce_InTheSameSession()
    {
        var handler = Peer(Replies(EmptyStream, AnswerStream));

        string answer = await AskAsync(handler);

        using var again = JsonDocument.Parse(handler.Requests[2].Body!);
        Assert.Multiple(() =>
        {
            Assert.That(answer, Does.StartWith("Fixed the null check; 14/14 tests pass."));
            Assert.That(Calls(handler).Count(call => call.EndsWith("/messages")), Is.EqualTo(2));
            Assert.That(again.RootElement.GetProperty("message").GetString(),
                Does.Contain("answer the task now from what you found"));
        });
    }

    /// <summary>Empty twice, it says so, and isn't asked a third time.</summary>
    [Test]
    public async Task EmptyTwice_SaysSo_AndDoesNotAskAgain()
    {
        var handler = Peer(Replies(EmptyStream, EmptyStream));

        string answer = await AskAsync(handler);

        Assert.Multiple(() =>
        {
            Assert.That(answer, Does.Contain("ended without an answer twice"));
            Assert.That(Calls(handler).Count(call => call.EndsWith("/messages")), Is.EqualTo(2));
        });
    }

    /// <summary>
    /// The files the model names go with the message, only those, their content from the session (F22).
    /// </summary>
    [Test]
    public async Task NamedAttachments_GoWithTheMessage()
    {
        var handler = Peer();
        var attached = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["verslag.txt"] = "Inkomste: R1 200 000",
            ["other.txt"] = "x"
        };

        using (SessionAttachments.Use(attached))
            await AskAsync(handler, "Som die verslag op.", ["verslag.txt"]);

        using var message = JsonDocument.Parse(handler.Requests[1].Body!);
        var sent = message.RootElement.GetProperty("attachments").EnumerateArray().ToList();
        Assert.That(
            sent.Select(file => (file.GetProperty("name").GetString(), file.GetProperty("content").GetString())),
            Is.EqualTo(new[] { ("verslag.txt", "Inkomste: R1 200 000") }));
    }

    /// <summary>A file the chat doesn't have is refused, and nothing is sent.</summary>
    [Test]
    public async Task AnUnknownAttachment_IsRefused_WithoutANetworkCall()
    {
        var handler = Peer();

        string answer = await AskAsync(handler, attachments: ["nowhere.txt"]);

        Assert.Multiple(() =>
        {
            Assert.That(answer, Does.Contain("nowhere.txt"));
            Assert.That(handler.Requests, Is.Empty);
        });
    }

    /// <summary>
    /// The user's place, the eval's replay and the turn that asked go with the opening, so the agent's time and "near
    /// me" are the user's, its pages share the recording (F18), and its usage links to the turn.
    /// </summary>
    [Test]
    public async Task ThePlace_TheReplay_AndTheParentTurn_GoWithTheOpening()
    {
        var handler = Peer();

        using (UserPlace.Use(new UserPlace("Pretoria, Gauteng, Suid-Afrika", "Africa/Johannesburg")))
        using (SessionReplay.Use(new SessionReplay(WebReplayMode.Replay, "run_1:beta:1/bobotie", Model: false)))
        using (UsageScope.Begin(session: "sess_parent", turn: 4, tags: new UsageTags(null, "run-1", "service")))
            await AskAsync(handler);

        var opening = JsonDocument.Parse(handler.Requests[0].Body!).RootElement;
        Assert.Multiple(() =>
        {
            Assert.That(opening.GetProperty("location").GetString(), Is.EqualTo("Pretoria, Gauteng, Suid-Afrika"));
            Assert.That(opening.GetProperty("time_zone").GetString(), Is.EqualTo("Africa/Johannesburg"));
            Assert.That(opening.GetProperty("replay").GetProperty("of").GetString(),
                Is.EqualTo("run_1:beta:1/bobotie"));
            Assert.That(opening.GetProperty("parent").GetString(), Is.EqualTo("sess_parent#4"));
            Assert.That(opening.GetProperty("run").GetString(), Is.EqualTo("run-1"));
        });
    }

    /// <summary>A message the peer refuses gives its reason, and the child is still deleted.</summary>
    [Test]
    public async Task AFailedMessage_StillDeletesTheChild()
    {
        var handler = Peer(() => Json(HttpStatusCode.InternalServerError,
            """{"type":"error","error":{"type":"api_error","message":"boom"}}"""));

        string answer = await AskAsync(handler);

        Assert.Multiple(() =>
        {
            Assert.That(answer, Does.Contain("(HTTP 500): boom"));
            Assert.That(handler.Requests.Last().Method, Is.EqualTo(HttpMethod.Delete));
        });
    }

    /// <summary>A peer that refuses to open the session gives its reason; nothing was created to delete.</summary>
    [Test]
    public async Task ARefusedOpening_GivesThePeersReason()
    {
        var handler = new StubHandler(_ => Json(HttpStatusCode.BadRequest,
            """{"type":"error","error":{"type":"invalid_request_error","message":"no such identity"}}"""));

        string answer = await AskAsync(handler);

        Assert.Multiple(() =>
        {
            Assert.That(answer, Does.Contain("HTTP 400").And.Contain("no such identity"));
            Assert.That(handler.Requests, Has.Count.EqualTo(1));
        });
    }

    /// <summary>A peer that answers with something other than its API's JSON is an answer, not a crash.</summary>
    [Test]
    public async Task AnUnreadableReply_IsAnAnswer_NotACrash()
    {
        var handler = new StubHandler(request => request.Method.Method switch
        {
            "POST" => new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("<html>proxy error</html>")
            },
            _ => new HttpResponseMessage(HttpStatusCode.OK)
        });

        Assert.That(await AskAsync(handler), Does.StartWith("Error: testprod's reply couldn't be read"));
    }
}
