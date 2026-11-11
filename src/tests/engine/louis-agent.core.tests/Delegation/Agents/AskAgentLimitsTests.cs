using LouisAgent.Composition.Packs;
using LouisAgent.Composition.Sessions;
using LouisAgent.Core.Delegation;
using LouisAgent.Core.Delegation.Agents;
using LouisAgent.Core.Sessions.SubSessions;
using LouisAgent.Core.Usage.Records;
using LouisAgent.Core.Usage.Sinks;
using Microsoft.Extensions.Time.Testing;
using static LouisAgent.Core.Tests.Delegation.FakePeer;

namespace LouisAgent.Core.Tests.Delegation.Agents;

/// <summary>
/// The limits an ask keeps (F127.3.1 step 11d, from the specialist tool's tests): a child that takes too long is
/// stopped and deleted, a turn's asks are capped (outside a turn, per minute), what a failed child was charged still
/// reaches the ledger, a follow-up that fails closes the conversation, and a chat keeps only so many open.
/// </summary>
public class AskAgentLimitsTests
{
    /// <summary>The asker, who may ask testprod in its voice and its specialist.</summary>
    private static readonly FakeIdentity Guide =
        new("guide", voice: "testprod/voice-en", calls: ["testprod", "testprod.specialist"]);

    /// <summary>The identities this instance runs.</summary>
    private static readonly IReadOnlyList<IAgentIdentity> Identities =
        [Guide, new FakeIdentity("testprod", voice: "testprod/voice-en")];

    /// <summary>The tools on this peer, with the test's timeout, clock and ledger.</summary>
    private static AskAgentTools Tools(HttpMessageHandler handler, TimeSpan? timeout = null,
        TimeProvider? time = null, IUsageSink? sink = null) =>
        new(Peers, Identities, "the-key", new HttpClient(handler), timeout, time, sink);

    /// <summary>Asks an address as the guide.</summary>
    private static async Task<string> AskAsync(AskAgentTools tools, string address = "testprod")
    {
        using var _ = TurnIdentity.Use(TurnIdentity.Of(Guide));
        return await tools.AskAgent(address, "Is a dog allowed?");
    }

    /// <summary>
    /// A child that doesn't answer in time is an error the model can handle; it is deleted, and nothing is charged.
    /// </summary>
    [Test]
    public async Task ATimeout_IsAnError_TheChildIsDeleted_AndNothingIsCharged()
    {
        var handler = new SlowPeer();
        var sink = new ListUsageSink();

        string answer;
        using (UsageScope.Begin(session: "sess_guide", turn: 1))
            answer = await AskAsync(Tools(handler, TimeSpan.FromMilliseconds(200), sink: sink));

        Assert.Multiple(() =>
        {
            Assert.That(answer, Does.Contain("didn't finish within"));
            Assert.That(handler.Deleted, Is.True);
            Assert.That(sink.Records, Is.Empty, "no usage arrived, so no zero record");
        });
    }

    /// <summary>Outside a turn (an MCP call) the cap is per minute, not for ever.</summary>
    [Test]
    public async Task OutsideATurn_TheCapIsPerMinute_NotForever()
    {
        var time = new FakeTimeProvider(DateTimeOffset.Parse("2026-10-13T10:00:00Z"));
        var tools = Tools(Peer(), time: time);

        for (int ask = 0; ask < AskAgentTools.MaxPerTurn; ask++)
            Assert.That(await AskAsync(tools), Does.Not.StartWith("Error"));
        string refused = await AskAsync(tools);
        time.Advance(CallSlots.NoTurnWindow);
        string later = await AskAsync(tools);

        Assert.Multiple(() =>
        {
            Assert.That(refused, Does.Contain("in the last minute"));
            Assert.That(later, Does.Not.StartWith("Error"), "a minute later it works again");
        });
    }

    /// <summary>A child that failed after some work still rolls up what it was charged (FX6).</summary>
    [Test]
    public async Task AFailedChild_StillRollsUpWhatItWasCharged()
    {
        var sink = new ListUsageSink();

        string answer;
        using (UsageScope.Begin(session: "sess_guide", turn: 1))
            answer = await AskAsync(Tools(Peer(Replies(FailedStream)), sink: sink));

        Assert.Multiple(() =>
        {
            Assert.That(answer, Does.StartWith("Error").And.Contain("the model refused"));
            Assert.That((sink.Records.Single().Requests, sink.Records.Single().Cost?.Amount),
                Is.EqualTo(((int?)2, (decimal?)0.002m)));
        });
    }

    /// <summary>
    /// A follow-up that fails leaves the child's turn in an unknown state, so the conversation is closed and the next
    /// ask starts clean.
    /// </summary>
    [Test]
    public async Task AFollowUpThatFails_ClosesTheConversation()
    {
        var handler = Peer(Replies(AnswerStream, FailedStream));
        var chat = new SubSessionRegistry();
        var tools = Tools(handler);

        string answer;
        using (SubSessionRegistry.Use(chat))
        {
            await AskAsync(tools);
            answer = await AskAsync(tools);
        }

        Assert.Multiple(() =>
        {
            Assert.That(answer, Does.Contain("the model refused"));
            Assert.That(handler.Requests.Last().Method, Is.EqualTo(HttpMethod.Delete));
            Assert.That(chat.Open, Is.Empty);
        });
    }

    /// <summary>A chat that keeps more than it may closes the oldest conversation on the peer.</summary>
    [Test]
    public async Task PastTheChatsCap_TheOldestConversation_IsDeletedOnThePeer()
    {
        var handler = PeerWithIds();
        var chat = new SubSessionRegistry(cap: 1);
        var tools = Tools(handler);

        using (SubSessionRegistry.Use(chat))
        using (UsageScope.Begin(session: "sess_guide", turn: 1))
        {
            await AskAsync(tools);
            await AskAsync(tools, "testprod.specialist");
        }

        Assert.Multiple(() =>
        {
            Assert.That(Calls(handler).Where(call => call.StartsWith("DELETE")),
                Is.EqualTo(new[] { "DELETE /v1/sessions/sess_1" }));
            Assert.That(chat.Open.Select(open => open.SessionId), Is.EqualTo(new[] { "sess_2" }));
        });
    }
}
