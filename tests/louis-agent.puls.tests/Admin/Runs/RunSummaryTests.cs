using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Puls.Admin.Runs;

namespace LouisAgent.Puls.Tests.Admin.Runs;

/// <summary>A stored run from the contract becomes the runs list's row.</summary>
public class RunSummaryTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 26, 8, 0, 0, TimeSpan.Zero);

    /// <summary>The row keeps the run's facts, its cost and requests, and the models it names.</summary>
    [Test]
    public void AStoredRun_BecomesItsRow()
    {
        var stored = Stored(new RunTotalRow(0.5m, "USD", 100, 4),
            [new RunModelRow("m", "agent", null, "haiku"), new RunModelRow("m", "specialist", "r", null)]);

        var row = RunSummary.Of(stored);

        Assert.That((row.Id, row.Check, row.Repeat, row.Status, row.StartedAt, row.StoreError, row.RescoreOf),
            Is.EqualTo(("run1", SampleEval.Beta, 2, "done", At, "late", "run0")));
        Assert.That(row.Total, Is.EqualTo(new TotalSummary(0.5m, 4)));
        Assert.That(row.Models, Is.EqualTo(new[] { "haiku" }));
    }

    /// <summary>A run from before totals and models were kept has neither.</summary>
    [Test]
    public void AnOldRun_HasNoTotalAndNoModels()
    {
        var row = RunSummary.Of(Stored(null, null));

        Assert.That((row.Total, row.Models), Is.EqualTo(((TotalSummary?)null, (IReadOnlyList<string>?)null)));
    }

    /// <summary>A stored run with this total and these models.</summary>
    private static StoredRunRow Stored(RunTotalRow? total, IReadOnlyList<RunModelRow>? models) =>
        new("run1", SampleEval.Beta, 2, "done", 0, At, null, total, "late", models, null, null, null, "run0", null);
}
