using LouisAgent.Evals.Contracts.Api;
using LouisAgent.Evals.Contracts.Runs;

namespace LouisAgent.Evals.Tests.Runs.Comparison;

/// <summary>
/// F126.4: which run is "the run before", asked of an eval's run list as the rows have it. A re-grade's is the run it
/// re-graded; a live run's or a replay's the newest earlier run of any kind that finished; a run that didn't finish
/// has none and is never one. The last eight finished runs, oldest first, and the criteria they missed most.
/// </summary>
public class RunHistoryTests
{
    private static readonly DateTimeOffset Day = new(2026, 11, 15, 9, 0, 0, TimeSpan.Zero);

    /// <summary>A live run's run before is the newest earlier one that finished, of any kind.</summary>
    [Test]
    public void ALiveRunsRunBefore_IsTheNewestEarlierFinishedRun_OfAnyKind()
    {
        var replay = Row(1, RunKind.Replay);
        var stopped = Row(2, status: RunStatus.Stopped);
        var live = Row(3);
        var history = new RunHistory([live, stopped, replay]);

        Assert.That(history.FindBefore(live), Is.EqualTo(replay));
        Assert.That(history.FindBefore(replay), Is.Null);
    }

    /// <summary>A re-grade's run before is the run it re-graded, even when a newer run finished between.</summary>
    [Test]
    public void ARegradesRunBefore_IsTheRunItRegraded()
    {
        var graded = Row(1);
        var between = Row(2);
        var regrade = Row(3, RunKind.Regrade) with { RegradeOf = graded.Id };

        Assert.That(new RunHistory([regrade, between, graded]).FindBefore(regrade), Is.EqualTo(graded));
    }

    /// <summary>A run that didn't finish has no run before.</summary>
    [TestCase(RunStatus.Running)]
    [TestCase(RunStatus.Stopped)]
    [TestCase(RunStatus.CouldNotFinish)]
    public void ARunThatDidNotFinish_HasNoRunBefore(RunStatus status)
    {
        var unfinished = Row(2, status: status);

        Assert.That(new RunHistory([unfinished, Row(1)]).FindBefore(unfinished), Is.Null);
    }

    /// <summary>The last eight finished runs, oldest first, and eight at most.</summary>
    [Test]
    public void FindLastFinished_IsOldestFirst_AndEightAtMost()
    {
        var rows = Enumerable.Range(1, 10).Select(day => Row(day)).Reverse().Append(Row(11, status: RunStatus.Stopped));

        var last = new RunHistory([.. rows]).FindLastFinished(8);

        Assert.That(last.Select(row => row.StartedAt.Day), Is.EqualTo(Enumerable.Range(18, 8)));
    }

    /// <summary>The criteria missed most over the last finished runs, added by their text, most missed first.</summary>
    [Test]
    public void TallyMissedMost_AddsTheRowsTallies_TheMostMissedFirst()
    {
        CriterionTally Pricing(int missed) => new("Links the pricing page", missed, 0);
        CriterionTally Area(int flipped) => new("Lights the area", 0, flipped);
        var rows = new[]
        {
            Row(3) with { Criteria = [Pricing(1), Area(1)] },
            Row(2) with { Criteria = [Pricing(1), new CriterionTally("Never answers no", 1, 0)] },
            Row(1) with { Criteria = [Area(1), new CriterionTally("Never answers no", 1, 0)] }
        };

        var most = new RunHistory(rows).TallyMissedMost(8);

        Assert.That(most.Take(2).Select(tally => (tally.Missed, tally.Flipped)), Has.All.EqualTo((2, 0)));
        Assert.That(most.Last(), Is.EqualTo(new CriterionTally("Lights the area", 0, 2)));
    }

    /// <summary>An eval's row carries its run before, and the change is the difference; none without one.</summary>
    [Test]
    public void AnEvalsRow_CountsTheChangeAgainstItsRunBefore()
    {
        var row = new EvalRow("guide", "louis-agent-guide", 7, Row(2) with { Passed = 6 })
        {
            RunBefore = Row(1) with { Passed = 5 }
        };

        Assert.That(row.CountChange(), Is.EqualTo(1));
        Assert.That((row with { RunBefore = null }).CountChange(), Is.Null);
    }

    /// <summary>A run's row from this day of November, live and finished unless a test says otherwise.</summary>
    private static RunRow Row(int day, RunKind kind = RunKind.Live, RunStatus status = RunStatus.Finished)
    {
        var at = Day.AddDays(day);
        return new RunRow($"{at:yyyyMMdd-HHmmss}-guide", kind, status, 3, at, at.AddMinutes(6), 4, 7);
    }
}
