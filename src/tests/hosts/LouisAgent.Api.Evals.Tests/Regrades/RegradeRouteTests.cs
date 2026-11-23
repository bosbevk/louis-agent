using System.Net;
using LouisAgent.Core.Sessions.Replay.Store;
using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Evals.Runs.Store;
using Microsoft.Extensions.AI;
using static LouisAgent.Api.Evals.Tests.EvalsTestHost;

namespace LouisAgent.Api.Evals.Tests.Regrades;

/// <summary>
/// F126.4: a kept run is re-graded at its own address, as a run of its own kind on the desk, one at a time. It names
/// the run it re-graded and the live run its answers came from, records the judge's new answers into that live run's
/// plays, and so a replay of the live run plays again after a criterion is reworded.
/// </summary>
public class RegradeRouteTests
{
    private const string Criterion = "Greets the person";
    private const string Reworded = "Greets the person warmly";

    /// <summary>
    /// A re-grade answers 202 with the new run's address, and the new run ends as a re-grade naming the run it
    /// re-graded and the live run behind it.
    /// </summary>
    [Test]
    public async Task ARegrade_IsAccepted_AndEndsAsARegradeOfTheRun()
    {
        await using var host = await StartAsync(Answering("Hello."), Judging(), criterion: Criterion);
        var live = await host.RunToTheEndAsync();

        using var started = await host.Client.PostAsync(RegradeOf(live.Id), null);
        var regrade = await host.EndedAsync(started.Headers.Location!);

        Assert.That(started.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
        Assert.That((regrade.Kind, regrade.Status, regrade.RegradeOf, regrade.ReplayOf),
            Is.EqualTo((RunKind.Regrade, RunStatus.Finished, live.Id, live.Id)));
        Assert.That(regrade.Passed, Is.EqualTo(live.Passed));
    }

    /// <summary>
    /// After a re-grade with a reworded criterion, a replay of the live run finishes instead of stopping on it, and a
    /// re-grade of that re-grade records into the same live run's play.
    /// </summary>
    [Test]
    public async Task AfterARegradeWithARewordedCriterion_AReplayFinishes_AndARegradeOfItRecordsIntoTheSamePlay()
    {
        await using var before = await StartAsync(Answering("Hello."), Judging(), criterion: Criterion);
        var live = await before.RunToTheEndAsync();
        await using var after = await StartAsync(Answering("Hello."), Judging(), criterion: Reworded,
            sameFoldersAs: before);

        using var started = await after.Client.PostAsync(RegradeOf(live.Id), null);
        var regrade = await after.EndedAsync(started.Headers.Location!);
        var replay = await after.RunToTheEndAsync(RunKind.Replay);
        await using var again = await StartAsync(Answering("Hello."), Judging(), criterion: "Greets them by name",
            sameFoldersAs: before);
        using var second = await again.Client.PostAsync(RegradeOf(regrade.Id), null);
        var regradeOfIt = await again.EndedAsync(second.Headers.Location!);
        var judged = await new ReplayStore(before.Kept.Recordings!).AsksAsync($"{live.Id}/hello.1",
            ReplayKind.Model);

        Assert.That((regrade.Status, replay.Status), Is.EqualTo((RunStatus.Finished, RunStatus.Finished)),
            replay.StoppedBecause);
        Assert.That((regradeOfIt.RegradeOf, regradeOfIt.ReplayOf), Is.EqualTo((regrade.Id, live.Id)));
        Assert.That(judged, Has.Count.EqualTo(4), "the agent's turn and three judge answers, one per wording");
    }

    /// <summary>A run the host doesn't have is a 404, and so is one whose eval is no longer here.</summary>
    [Test]
    public async Task AnUnknownRun_OrOneWhoseEvalIsGone_IsNotFound()
    {
        await using var host = await StartAsync(Answering("Hello."));
        var started = new DateTimeOffset(2026, 11, 23, 4, 30, 12, TimeSpan.Zero);
        var gone = new EvalRun("20261123-043012-gone", "gone", Agent, RunKind.Live, 1, started, [])
        {
            Status = RunStatus.Finished,
            EndedAt = started.AddMinutes(1)
        };
        new RunStore(host.RunsFolder).Save(gone);

        using var unknown = await host.Client.PostAsync(RegradeOf("20261123-043012-nobody"), null);
        using var evalGone = await host.Client.PostAsync(RegradeOf(gone.Id), null);

        Assert.That((unknown.StatusCode, evalGone.StatusCode),
            Is.EqualTo((HttpStatusCode.NotFound, HttpStatusCode.NotFound)));
        Assert.That(await evalGone.Content.ReadAsStringAsync(), Does.Contain("gone"));
    }

    /// <summary>While any run goes a re-grade is refused with a 409; with no data folder it is a 400.</summary>
    [Test]
    public async Task ARegrade_WhileARunGoes_IsAConflict_AndWithNoDataFolder_ABadRequest()
    {
        using var held = new ManualResetEventSlim();
        await using var host = await StartAsync(Answering("Hello."));
        var kept = await host.RunToTheEndAsync();
        await using var waiting = await StartAsync(Waiting(held), sameFoldersAs: host);
        using var going = await waiting.StartRunAsync();
        await using var nowhere = await StartAsync(Answering("Hello."), withDataFolder: false);

        using var conflict = await waiting.Client.PostAsync(RegradeOf(kept.Id), null);
        using var noFolder = await nowhere.Client.PostAsync(RegradeOf(kept.Id), null);
        held.Set();
        await waiting.EndedAsync(going.Headers.Location!);

        Assert.That((conflict.StatusCode, noFolder.StatusCode),
            Is.EqualTo((HttpStatusCode.Conflict, HttpStatusCode.BadRequest)));
    }

    /// <summary>The route answers 401 without the admin's key.</summary>
    [Test]
    public async Task ARegrade_WithoutTheAdminKey_IsUnauthorized()
    {
        await using var host = await StartAsync(Answering("Hello."));
        host.Client.DefaultRequestHeaders.Remove("x-api-key");

        using var response = await host.Client.PostAsync(RegradeOf("20261123-043012-greets"), null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    /// <summary>Stop stops a re-grade, as it stops a run: it is kept as stopped.</summary>
    [Test]
    public async Task Stop_StopsARegrade()
    {
        using var held = new ManualResetEventSlim();
        await using var before = await StartAsync(Answering("Hello."), Judging(), criterion: Criterion);
        var live = await before.RunToTheEndAsync();
        await using var after = await StartAsync(Answering("Hello."), CancelledWhenLetGo(held), criterion: Reworded,
            sameFoldersAs: before);

        using var started = await after.Client.PostAsync(RegradeOf(live.Id), null);
        using var stop = await after.Client.DeleteAsync(started.Headers.Location!);
        held.Set();
        var stopped = await after.EndedAsync(started.Headers.Location!);

        Assert.That(stop.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That((stopped.Kind, stopped.Status), Is.EqualTo((RunKind.Regrade, RunStatus.Stopped)));
    }

    /// <summary>
    /// A judge's model that waits until the test lets it go, then gives up as a real client does when its call was
    /// cancelled: this fake can't see the token, so the test lets it go once the stop is sent.
    /// </summary>
    private static FakeChatClient CancelledWhenLetGo(ManualResetEventSlim held) => new(_ =>
    {
        held.Wait(TimeSpan.FromSeconds(30));
        throw new OperationCanceledException();
    });

    /// <summary>The address that re-grades a kept run.</summary>
    private static string RegradeOf(string id) => $"/v1/admin/evals/runs/{id}/regrade";

    /// <summary>A judge's model that marks every criterion met.</summary>
    private static FakeChatClient Judging() => Answering("""{"reason":"It greets them.","met":true}""");

    /// <summary>A model that waits until the test lets it answer, so the run stays going.</summary>
    private static FakeChatClient Waiting(ManualResetEventSlim held) => new(Enumerable.Repeat<Func<IList<ChatMessage>,
        ChatResponse>>(_ =>
    {
        held.Wait(TimeSpan.FromSeconds(30));
        return new ChatResponse(new ChatMessage(ChatRole.Assistant, """{"reason":"It greets them.","met":true}"""));
    }, 5).ToArray());
}
