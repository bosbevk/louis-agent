using LouisAgent.Api.Sessions;
using LouisAgent.Choices;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Llm.Replay;
using LouisAgent.Core.Skills.Providers;
using LouisAgent.Core.Usage.Records;
using LouisAgent.SessionStream;
using Microsoft.Extensions.AI;

namespace LouisAgent.Api.Tests.Sessions;

/// <summary>
/// A failed turn's error event says what kind of failure it was (FX26): a replay with no recorded answer streams
/// <c>replay_miss_error</c>, so a reader can mark the turn a replay miss; any other failure still streams
/// <c>api_error</c>, as before.
/// </summary>
public class TurnFailureKindTests
{
    /// <summary>Streams one turn whose model request throws this, and hands back the stream's data.</summary>
    private static async Task<string> FailedTurnAsync(Exception thrown)
    {
        var engine = new AgentEngine(new CompositeSkillProvider(), new FakeChatClient(_ => throw thrown),
            TestPaths.Agent(), toolsets: [], usageSink: new ListUsageSink());
        var session = new AgentSession("sess_fail", engine, [CoreToolset.Web.Wire()], 0, UsageTags.None);
        Assert.That(session.TryStartTurn(CancellationToken.None, out var turn), Is.True);

        var written = new List<string>();
        await foreach (var item in TurnStream.StreamTurnAsync(engine, session, "Wat kos bobotie?", turn))
            written.Add(item.Data ?? "");
        return string.Join("\n", written);
    }

    /// <summary>A replay miss streams its own kind, with its message.</summary>
    [Test]
    public async Task AReplayMiss_StreamsTheReplayMissKind()
    {
        string stream = await FailedTurnAsync(new ModelReplayMissException("No recorded model answer here."));

        Assert.That(stream, Does.Contain($"\"type\":\"{StreamErrorKind.ReplayMiss.Wire()}\"")
            .And.Contain("No recorded model answer here."));
    }

    /// <summary>Any other failure streams the API's own kind, as it always did.</summary>
    [Test]
    public async Task AnyOtherFailure_StreamsTheApisKind()
    {
        string stream = await FailedTurnAsync(new InvalidOperationException("Something else broke."));

        Assert.That(stream, Does.Contain($"\"type\":\"{StreamErrorKind.Api.Wire()}\"")
            .And.Not.Contain(StreamErrorKind.ReplayMiss.Wire()));
    }
}
