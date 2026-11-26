using LouisAgent.Api.Endpoints.Sessions;
using LouisAgent.Api.Sessions;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Skills.Providers;
using LouisAgent.Core.Usage.Records;
using Microsoft.Extensions.AI;

namespace LouisAgent.Api.Tests.Sessions;

/// <summary>
/// The engine's commands, <c>/tools</c>, <c>/approve</c>, <c>/reject</c> and <c>/usage</c>, are the operator's: a site
/// visitor's chat, which the open door admitted and tagged, sends them to the model as words (FX255).
/// </summary>
public class TurnCommandsTests
{
    /// <summary>A visitor's chat: opened through the open door, tagged for the ledger as the door's.</summary>
    private static readonly UsageTags Visitors = new(null, null, SessionHandlers.OpenDoorService);

    /// <summary>
    /// What came back, and whether the model was asked at all, for one message in a session so tagged.
    /// </summary>
    private static async Task<(bool Asked, string Streamed)> SendAsync(string message, UsageTags tags)
    {
        bool asked = false;
        var model = new FakeChatClient(_ =>
        {
            asked = true;
            return new ChatResponse(new ChatMessage(ChatRole.Assistant, "The model's answer."));
        });
        var engine = new AgentEngine(new CompositeSkillProvider(), model, TestPaths.Agent(), toolsets: [],
            usageSink: new ListUsageSink());
        var session = new AgentSession("sess_commands", engine, [], 0, tags);
        Assert.That(session.TryStartTurn(CancellationToken.None, out var turn), Is.True);

        var streamed = new List<string>();
        await foreach (var item in TurnStream.StreamTurnAsync(engine, session, message, turn)) streamed.Add(item.Data);
        return (asked, string.Concat(streamed));
    }

    /// <summary>A visitor's /tools is a message like any other: the model answers it, not the engine.</summary>
    [Test]
    public async Task AVisitorsCommand_GoesToTheModel()
    {
        var (asked, streamed) = await SendAsync("/tools", Visitors);

        Assert.Multiple(() =>
        {
            Assert.That(asked, Is.True);
            Assert.That(streamed, Does.Contain("The model's answer."));
            Assert.That(streamed, Does.Not.Contain("/approve"));
        });
    }

    /// <summary>The operator's /tools is still answered by the engine at once, and the model isn't asked.</summary>
    [Test]
    public async Task TheOperatorsCommand_IsAnsweredByTheEngine()
    {
        var (asked, streamed) = await SendAsync("/tools", UsageTags.None);

        Assert.Multiple(() =>
        {
            Assert.That(asked, Is.False);
            Assert.That(streamed, Does.Contain("/approve"));
        });
    }
}
