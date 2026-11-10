using System.Net.Http.Json;
using System.Text.Json;
using LouisAgent.Api.Sessions;
using LouisAgent.Composition.Sessions;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Skills.Providers;
using LouisAgent.Core.Usage.Records;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Api.Tests.Sessions;

/// <summary>
/// F127.2 (Louis's pick "the identity says so"): a session knows its identity and whether it is a demonstration, and
/// each of its turns runs as that identity, so a tool in the turn (the leads pack's) can tell a demonstration's
/// booking from a real one.
/// </summary>
public class TurnIdentityTests
{
    /// <summary>A showroom business: a demonstration, on an embedded voice.</summary>
    private static readonly FakeIdentity Shop = new("shop", voice: "puls/voice-en", demonstration: true);

    /// <summary>A session opened as a demonstration identity keeps it, and says it's a demonstration.</summary>
    [Test]
    public async Task ASession_KnowsItsIdentity_AndThatItIsADemonstration()
    {
        await using var app = ApiTestHost.Build(moreIdentities: [Shop]);
        await app.StartAsync();

        using var opened = await app.GetTestClient().PostAsJsonAsync("/v1/sessions", new { identity = "shop" });
        string id = JsonDocument.Parse(await opened.Content.ReadAsStringAsync()).RootElement
            .GetProperty("session_id").GetString()!;

        Assert.That(app.Services.GetRequiredService<AgentSessions>().Get(id)!.Who,
            Is.EqualTo(new TurnIdentity("shop", true)));
    }

    /// <summary>A turn runs as its session's identity: the model's request, inside the turn, sees it.</summary>
    [Test]
    public async Task ATurn_RunsAsItsSessionsIdentity()
    {
        TurnIdentity? seen = null;
        var model = new FakeChatClient(_ =>
        {
            seen = TurnIdentity.Current;
            return new ChatResponse(new ChatMessage(ChatRole.Assistant, "Hello."));
        });
        var engine = new AgentEngine(new CompositeSkillProvider(), model, TestPaths.Agent(), toolsets: [],
            usageSink: new ListUsageSink());
        var session = new AgentSession("sess_who", engine, [], 0, UsageTags.None,
            who: new TurnIdentity("shop", true));
        Assert.That(session.TryStartTurn(CancellationToken.None, out var turn), Is.True);

        await foreach (var _ in TurnStream.StreamTurnAsync(engine, session, "Hi", turn))
        {
        }

        Assert.Multiple(() =>
        {
            Assert.That(seen, Is.EqualTo(new TurnIdentity("shop", true)));
            Assert.That(TurnIdentity.Current, Is.Null, "only for the turn");
        });
    }
}
