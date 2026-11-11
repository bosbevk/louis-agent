using System.Net;
using System.Text;
using System.Text.Json;
using LouisAgent.Composition.Packs;
using LouisAgent.Composition.Sessions;
using LouisAgent.Core.Delegation.Agents;
using LouisAgent.Core.Sessions.SubSessions;
using LouisAgent.Core.Usage.Records;
using static LouisAgent.Core.Tests.Delegation.FakePeer;

namespace LouisAgent.Core.Tests.Delegation.Agents;

/// <summary>
/// Asking the same agent again in a chat carries on the conversation it already has (F127.3.1 step 8): the chat keeps
/// one child per address, a busy one stays open and the model is told to wait, and one the peer dropped is started
/// again in the same call.
/// </summary>
public class AskAgentFollowUpTests
{
    /// <summary>The asker, who may ask testprod in its voice and its specialist.</summary>
    private static readonly FakeIdentity Guide =
        new("guide", voice: "testprod/voice-en", calls: ["testprod", "testprod.specialist"]);

    /// <summary>The identities this instance runs.</summary>
    private static readonly IReadOnlyList<IAgentIdentity> Identities =
        [Guide, new FakeIdentity("testprod", voice: "testprod/voice-en")];

    /// <summary>What the peer says while the session is still answering, as the API does.</summary>
    private const string Busy =
        """{"type":"error","error":{"type":"conflict_error","message":"Session sess_child is still answering."}}""";

    /// <summary>The tools on the stub peer.</summary>
    private static AskAgentTools Tools(StubHandler handler) =>
        new(Peers, Identities, "the-key", new HttpClient(handler));

    /// <summary>Asks each address in turn as the guide, in one chat and one turn, and gives back the answers.</summary>
    private static async Task<List<string>> AskInOneChat(AskAgentTools tools, SubSessionRegistry chat,
        params (string Address, string Message)[] asks)
    {
        var answers = new List<string>();
        using var _ = SubSessionRegistry.Use(chat);
        using var turn = UsageScope.Begin(session: "sess_guide", turn: 1);
        using var who = TurnIdentity.Use(TurnIdentity.Of(Guide));
        foreach (var (address, message) in asks)
            answers.Add(await tools.AskAgent(address, message));
        return answers;
    }

    /// <summary>
    /// A peer that meets each message with the next status given: OK is the answer, anything else that status with
    /// <paramref name="body"/>.
    /// </summary>
    private static StubHandler Meets(string body, params HttpStatusCode[] statuses)
    {
        int sent = 0;
        return Peer(() => statuses[sent++] == HttpStatusCode.OK
            ? new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(AnswerStream, Encoding.UTF8, "text/event-stream")
            }
            : Json(statuses[sent - 1], body));
    }

    /// <summary>
    /// A second ask to the same address goes to the child the chat kept, as a message in its conversation; the chat
    /// still keeps the one child.
    /// </summary>
    [Test]
    public async Task ASecondAsk_GoesOnTheKeptConversation()
    {
        var handler = Peer();
        var chat = new SubSessionRegistry();

        var answers = await AskInOneChat(Tools(handler), chat, ("testprod", "Is a dog allowed?"),
            ("TestProd", "Book the garden room for Friday."));

        using var followUp = JsonDocument.Parse(handler.Requests[2].Body!);
        Assert.Multiple(() =>
        {
            Assert.That(Calls(handler),
                Is.EqualTo(new[]
                {
                    "POST /v1/sessions", "POST /v1/sessions/sess_child/messages",
                    "POST /v1/sessions/sess_child/messages"
                }));
            Assert.That(followUp.RootElement.GetProperty("message").GetString(),
                Is.EqualTo("Book the garden room for Friday."));
            Assert.That(chat.Open.Single().Address?.ToString(), Is.EqualTo("testprod"));
            Assert.That(answers[1], Does.EndWith("[Answered by testprod]"));
        });
    }

    /// <summary>Each address has a conversation of its own, and a later ask picks the one with its address.</summary>
    [Test]
    public async Task EachAddress_HasItsOwnConversation()
    {
        var handler = PeerWithIds();
        var chat = new SubSessionRegistry();

        await AskInOneChat(Tools(handler), chat, ("testprod", "a"), ("testprod.specialist", "b"), ("testprod", "c"));

        Assert.That(Calls(handler).Where(call => call.EndsWith("/messages")),
            Is.EqualTo(new[]
            {
                "POST /v1/sessions/sess_1/messages", "POST /v1/sessions/sess_2/messages",
                "POST /v1/sessions/sess_1/messages"
            }));
    }

    /// <summary>
    /// A child still answering (409) is not closed: it stays kept, and the model is told to wait and ask again.
    /// </summary>
    [Test]
    public async Task ABusyAgent_StaysOpen_AndTheModelIsToldToWait()
    {
        var handler = Meets(Busy, HttpStatusCode.OK, HttpStatusCode.Conflict);
        var chat = new SubSessionRegistry();

        var answers = await AskInOneChat(Tools(handler), chat, ("testprod", "a"), ("testprod", "b"));

        Assert.Multiple(() =>
        {
            Assert.That(answers[1],
                Does.StartWith("Error: testprod is still answering").And.Contain("Wait for that answer"));
            Assert.That(Calls(handler), Has.None.StartsWith("DELETE"));
            Assert.That(chat.Open, Has.Count.EqualTo(1));
        });
    }

    /// <summary>
    /// A child the peer dropped (404) is forgotten, and the same ask opens a new conversation in the same call, whose
    /// answer says it is new.
    /// </summary>
    [Test]
    public async Task ADroppedConversation_IsStartedAgain_InTheSameCall()
    {
        var handler = Meets("{}", HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.OK);
        var chat = new SubSessionRegistry();

        var answers = await AskInOneChat(Tools(handler), chat, ("testprod", "a"), ("testprod", "b"));

        Assert.Multiple(() =>
        {
            Assert.That(Calls(handler).Count(call => call == "POST /v1/sessions"), Is.EqualTo(2));
            Assert.That(answers[1], Does.Contain("new conversation").And.Not.Contain("StartSubSession"));
            Assert.That(chat.Open, Has.Count.EqualTo(1));
        });
    }
}
