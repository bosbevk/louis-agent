using System.Net;
using LouisAgent.Core.Sessions.Replay.Store;
using LouisAgent.Evals.Contracts.Runs;
using static LouisAgent.Api.Evals.Tests.EvalsTestHost;

namespace LouisAgent.Api.Evals.Tests;

/// <summary>
/// A live run records as it goes, the agent's answers and the judge's under each play's trial, and a replay plays
/// the eval's last live run back for free: the same marks, no model called, a file of its own. A recording that no
/// longer answers stops the replay and says a live run is needed.
/// </summary>
public class ReplayRunTests
{
    private const string Criterion = "Greets the person";

    /// <summary>A live run's plays record the agent's answers and the judge's, each under its own trial.</summary>
    [Test]
    public async Task ALiveRun_RecordsTheAgentAndTheJudge_UnderEachPlaysTrial()
    {
        await using var host = await StartAsync(Answering("Hello."), Judging(), criterion: Criterion);

        var run = await host.RunToTheEndAsync();

        var recorded = await new ReplayStore(host.Kept.Recordings!).AsksAsync($"{run.Id}/hello.1", ReplayKind.Model);
        Assert.That(run.Status, Is.EqualTo(RunStatus.Finished));
        Assert.That(recorded, Has.Count.EqualTo(2), "the agent's turn and the judge's mark");
    }

    /// <summary>A replay ends finished, of the kind replay, with the live run's marks, calling neither model.</summary>
    [Test]
    public async Task AReplay_EndsFinished_WithTheSameMarks_AndCallsNoModel()
    {
        var agentCalls = new Count();
        var judgeCalls = new Count();
        await using var host = await StartAsync(Answering("Hello.", agentCalls), Judging(judgeCalls),
            criterion: Criterion);
        var live = await host.RunToTheEndAsync();
        var called = (agentCalls.Calls, judgeCalls.Calls);

        var replay = await host.RunToTheEndAsync(RunKind.Replay);

        Assert.That((replay.Status, replay.Kind, replay.ReplayOf), Is.EqualTo((RunStatus.Finished, RunKind.Replay,
            live.Id)));
        Assert.That(Marks(replay), Is.EqualTo(Marks(live)));
        Assert.That((agentCalls.Calls, judgeCalls.Calls), Is.EqualTo(called), "no model was called again");
        Assert.That(File.Exists(Path.Combine(host.RunsFolder, replay.Id + ".json")), Is.True);
    }

    /// <summary>A replay with no finished live run of the eval before it is refused, saying so.</summary>
    [Test]
    public async Task AReplay_WithNoLiveRunBeforeIt_IsABadRequest()
    {
        await using var host = await StartAsync(Answering("Hello."));

        using var response = await host.StartRunAsync(kind: RunKind.Replay);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(await response.Content.ReadAsStringAsync(), Does.Contain("live run"));
    }

    /// <summary>
    /// A criterion reworded since the live run asks the judge something the recording doesn't hold: the replay ends
    /// as could not finish, saying the recording is out of date and a live run is needed.
    /// </summary>
    [Test]
    public async Task AReplayWhoseRecordingNoLongerAnswers_CouldNotFinish_SayingALiveRunIsNeeded()
    {
        await using var before = await StartAsync(Answering("Hello."), Judging(), criterion: Criterion);
        await before.RunToTheEndAsync();
        await using var after = await StartAsync(Answering("Hello."), Judging(), criterion: "Greets the person warmly",
            sameFoldersAs: before);

        var replay = await after.RunToTheEndAsync(RunKind.Replay);

        Assert.That(replay.Status, Is.EqualTo(RunStatus.CouldNotFinish));
        Assert.That(replay.StoppedBecause, Does.Contain("out of date").And.Contain("live run"));
    }

    /// <summary>A live start on a host that keeps no recordings is refused, since it could never be replayed.</summary>
    [Test]
    public async Task ALiveStart_OnAHostWithNoRecordings_IsABadRequest()
    {
        await using var host = await StartAsync(Answering("Hello."), withRecordings: false);

        using var response = await host.StartRunAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(await response.Content.ReadAsStringAsync(), Does.Contain("keeps no recordings"));
    }

    /// <summary>A judge's model that marks every criterion met.</summary>
    private static FakeChatClient Judging(Count? count = null) =>
        Answering("""{"reason":"It greets them.","met":true}""", count);

    /// <summary>Every mark of a run, by conversation and turn, as text and met.</summary>
    private static IReadOnlyList<(string, bool)> Marks(EvalRun run) =>
        run.Conversations.SelectMany(conversation => conversation.Repeats).SelectMany(repeat => repeat.Turns)
            .SelectMany(turn => turn.Facts.Concat(turn.Criteria)).Select(mark => (mark.Text, mark.Met)).ToList();
}
