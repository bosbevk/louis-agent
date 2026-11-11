using System.Text.Json;
using LouisAgent.Composition.Packs;
using LouisAgent.Composition.Sessions;
using LouisAgent.Core.Delegation.Agents;
using LouisAgent.Core.Sessions.SubSessions;
using LouisAgent.Core.Usage.Records;
using LouisAgent.Core.Usage.Sinks;
using static LouisAgent.Core.Tests.Delegation.FakePeer;

namespace LouisAgent.Core.Tests.Delegation.Agents;

/// <summary>
/// <c>AskAgent</c> starts a conversation with another agent by its address (F127.3.1 step 7), on the tests' own
/// prompts: the guide may ask <c>testprod</c> in its voice and its specialist; the peer is a stub, so nothing goes
/// over the network. Every check is in code, before anything is sent.
/// </summary>
public class AskAgentToolsTests
{
    /// <summary>The asker, its <c>Calls</c> naming a voice, a specialist and a role no folder holds.</summary>
    private static readonly FakeIdentity Guide =
        new("guide", voice: "testprod/voice-en", calls: ["testprod", "testprod.specialist", "testprod.cook"]);

    /// <summary>The identities this instance runs.</summary>
    private static readonly IReadOnlyList<IAgentIdentity> Identities =
        [Guide, new FakeIdentity("testprod", voice: "testprod/voice-en")];

    /// <summary>The tools on the stub peer, with the test's ledger.</summary>
    private static AskAgentTools Tools(StubHandler handler, IUsageSink? sink = null) =>
        new(Peers, Identities, "the-key", new HttpClient(handler), usageSink: sink);

    /// <summary>Asks as the guide, in a turn of its own.</summary>
    private static async Task<string> AskAsGuide(AskAgentTools tools, string address)
    {
        using var _ = TurnIdentity.Use(TurnIdentity.Of(Guide));
        return await tools.AskAgent(address, "Is a dog allowed?");
    }

    /// <summary>The opening the peer was sent.</summary>
    private static JsonElement Opening(StubHandler handler) =>
        JsonDocument.Parse(handler.Requests[0].Body!).RootElement;

    /// <summary>
    /// An identity is opened one level down in its own voice, with no toolsets sent so it loads its own packs, and
    /// its answer comes back under a line that names it.
    /// </summary>
    [Test]
    public async Task AnIdentity_IsAskedInItsVoice_OneLevelDown()
    {
        var handler = Peer();

        string answer = await AskAsGuide(Tools(handler), "testprod");

        var opening = Opening(handler);
        Assert.Multiple(() =>
        {
            Assert.That(opening.GetProperty("identity").GetString(), Is.EqualTo("testprod"));
            Assert.That(opening.GetProperty("role").GetString(), Is.EqualTo("testprod/voice-en"));
            Assert.That(opening.GetProperty("depth").GetInt32(), Is.EqualTo(1));
            Assert.That(opening.GetProperty("toolsets").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(opening.GetProperty("task").GetString(), Is.EqualTo("agent:testprod"));
            Assert.That(answer, Does.StartWith("Fixed the null check; 14/14 tests pass.")
                .And.EndWith("[Answered by testprod, did: no tools]"));
        });
    }

    /// <summary>A role is opened as the role file named for its identity.</summary>
    [Test]
    public async Task ARole_IsAskedAsItsRoleFile()
    {
        var handler = Peer();

        await AskAsGuide(Tools(handler), "testprod.specialist");

        var opening = Opening(handler);
        Assert.That((opening.GetProperty("identity").GetString(), opening.GetProperty("role").GetString()),
            Is.EqualTo(("testprod", "testprod-specialist")));
    }

    /// <summary>
    /// Text that is no address, an address the asker may not call, and one this instance can't reach are refused
    /// with the reason, and nothing is sent.
    /// </summary>
    [TestCase("testprod.specialist.extra", "no agent's address")]
    [TestCase("other", "may not call other")]
    [TestCase("testprod.cook", "has no role 'cook'")]
    public async Task ABadAddress_IsRefused_BeforeAnythingIsSent(string address, string reason)
    {
        var handler = Peer();

        string answer = await AskAsGuide(Tools(handler), address);

        Assert.Multiple(() =>
        {
            Assert.That(handler.Requests, Is.Empty);
            Assert.That(answer, Does.StartWith("Error").And.Contain(reason));
        });
    }

    /// <summary>A chat that runs as no agent calls nobody.</summary>
    [Test]
    public async Task AChatOfNoAgent_CallsNobody()
    {
        var handler = Peer();

        string answer = await Tools(handler).AskAgent("testprod", "Is a dog allowed?");

        Assert.That((answer.StartsWith("Error", StringComparison.Ordinal), handler.Requests.Count),
            Is.EqualTo((true, 0)));
    }

    /// <summary>One turn asks at most three times; the fourth is refused in code.</summary>
    [Test]
    public async Task AFourthAskInOneTurn_IsRefused()
    {
        var tools = Tools(Peer());
        var answers = new List<string>();

        using (UsageScope.Begin(session: "sess_guide", turn: 1))
            for (int ask = 0; ask < 4; ask++)
                answers.Add(await AskAsGuide(tools, "testprod"));

        Assert.That(answers.Select(a => a.StartsWith("Error", StringComparison.Ordinal)),
            Is.EqualTo(new[] { false, false, false, true }));
    }

    /// <summary>What the asked agent used is one ledger row in the asker's turn.</summary>
    [Test]
    public async Task WhatItUsed_IsRolledUpIntoTheAskersTurn()
    {
        var sink = new ListUsageSink();

        using (UsageScope.Begin(session: "sess_guide", turn: 1))
            await AskAsGuide(Tools(Peer(), sink), "testprod");

        Assert.That((sink.Records.Single().Model, sink.Records.Single().Requests),
            Is.EqualTo(("agent:testprod", (int?)3)));
    }

    /// <summary>The asked agent's work streams live to whoever shows the turn, labelled with its address.</summary>
    [Test]
    public async Task ItsWork_StreamsLive_UnderItsAddress()
    {
        var events = new List<SubSessionEvent>();

        using (SubSessionFeed.Observe(events.Add))
            await AskAsGuide(Tools(Peer()), "testprod");

        Assert.That(events, Is.Not.Empty);
        Assert.That(events.Select(e => e.Agent).Distinct(), Is.EqualTo(new[] { "testprod" }));
    }
}
