using LouisAgent.Choices;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Api.Sessions;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Sessions.Replay;
using LouisAgent.Core.Skills.Providers;
using LouisAgent.Core.Usage.Records;
using Microsoft.Extensions.AI;

namespace LouisAgent.Api.Tests.Sessions;

// F18 step 4a.2: a session's replay is the one in force while its turn runs, as its place is (the browser reads SessionReplay.Current
// on every call). A tool that reports it shows what the turn saw. Written first: TurnStream didn't set it on the red run.
public class TurnReplayTests
{
    private sealed class WhichTrialTools
    {
        [System.ComponentModel.Description("Says which recorded trial this turn reads or writes")]
        public string WhichTrial() => SessionReplay.Current is { } replay ? $"{replay.Web}:{replay.Of}" : "none";
    }

    private static async Task<string> RunTurnAsync(SessionReplay? replay)
    {
        var engine = new AgentEngine(new CompositeSkillProvider(), new FakeChatClient(
                _ => new ChatResponse(new ChatMessage(ChatRole.Assistant,
                    [new FunctionCallContent("call_1", "WhichTrial", new Dictionary<string, object?>())])),
                _ => new ChatResponse(new ChatMessage(ChatRole.Assistant, [new TextContent("done")]))),
            TestPaths.Agent(), toolsets: [new WhichTrialTools()], usageSink: new ListUsageSink());
        var session = new AgentSession("sess_replay", engine, [CoreToolset.Web.Wire()], 0, UsageTags.None,
            replay: replay);
        Assert.That(session.TryStartTurn(CancellationToken.None, out var turn), Is.True);

        var written = new List<string>();
        await foreach (var item in TurnStream.StreamTurnAsync(engine, session, "Which trial?", turn))
            written.Add(item.Data ?? "");
        Assert.That(SessionReplay.Current, Is.Null, "the turn leaves nothing behind in the caller's flow");
        return string.Join("\n", written);
    }

    [Test]
    public async Task ASessionsReplay_IsInForceWhileItsTurnRuns()
    {
        string stream = await RunTurnAsync(new SessionReplay(WebReplayMode.Record, "run_1:puls-cook:1/bobotie", false));

        Assert.That(stream, Does.Contain("Record:run_1:puls-cook:1/bobotie"));
    }

    [Test]
    public async Task ASessionWithNoReplay_RunsItsTurnWithNone()
    {
        string stream = await RunTurnAsync(null);

        Assert.That(stream, Does.Contain("none").And.Not.Contain("Record:"));
    }
}
