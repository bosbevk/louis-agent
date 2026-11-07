using LouisAgent.Api.Contracts.Admin.Checks;
using LouisAgent.Api.Contracts.Admin.Evals;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Web.Admin.Evals.List;

namespace LouisAgent.Web.Tests.Admin.Evals.List;

/// <summary>
/// What the Evals page's list says per eval (F53.2 step 2), worked out before any markup: the last run's status and
/// when it ran, per mode its pass@k with the change against the run before, and which eval opens when the address
/// names none.
/// </summary>
public class EvalListViewTests
{
    /// <summary>When the runs started.</summary>
    private static readonly DateTimeOffset T0 = new(2026, 11, 1, 9, 0, 0, TimeSpan.Zero);

    /// <summary>A finished run at this time with these modes' pass@k (none for a run from before the rates).</summary>
    private static EvalRunRow Run(string id, DateTimeOffset at, params (string Mode, double? PassAtK)[] modes) =>
        new(new StoredRunRow(id, "cook", 1, "done", 0, at, at.AddMinutes(9), null, null, null, "cook", 10, null,
                null, null),
            [.. modes.Select(m =>
                new ModeRow(m.Mode, 1, [], null, null, m.PassAtK, m.PassAtK, m.PassAtK is null ? null : 1, 0, 0))]);

    /// <summary>An eval with these runs, newest first, as the API lists them.</summary>
    private static EvalRow Eval(string name, params EvalRunRow[] runs) =>
        new(name, SampleIdentity.Alpha, 10, $"{name} proves", false, null, [], [], [], [], runs.Length == 0, runs);

    /// <summary>The list reply with these evals.</summary>
    private static EvalListReply List(params EvalRow[] evals) => new(evals, new StoreRow(true, null));

    /// <summary>
    /// Each line says its last run's status and per mode the pass@k with the change against the run before.
    /// </summary>
    [Test]
    public void EachLine_SaysItsLastRun_AndTheChangePerMode()
    {
        var eval = Eval("cook",
            Run("run3", T0, (SampleMode.Alpha, 0.75), (SampleMode.Beta, 0.5)),
            Run("run2", T0.AddDays(-1), (SampleMode.Alpha, 0.5)),
            Run("run1", T0.AddDays(-2), (SampleMode.Alpha, 0.25), (SampleMode.Beta, 0.5)));

        var line = EvalListView.Of(List(eval)).Evals.Single();

        Assert.That((line.Name, line.Proves, line.Status, line.LastAt),
            Is.EqualTo(("cook", "cook proves", "done", (DateTimeOffset?)T0)));
        Assert.That(line.Modes.Select(m => (m.Mode, m.PassAtK, m.Change)),
            Is.EqualTo(new[] { (SampleMode.Alpha, (double?)0.75, (double?)0.25), (SampleMode.Beta, 0.5, 0.0) }),
            "a mode's run before is the newest earlier run that has it");
    }

    /// <summary>A mode with no earlier run, or whose run before has no rates, has no change to say.</summary>
    [Test]
    public void AModeWithNoRunBefore_HasNoChange()
    {
        var eval = Eval("cook", Run("run2", T0, (SampleMode.Alpha, 1.0)),
            Run("run1", T0.AddDays(-1), (SampleMode.Alpha, null)));

        var line = EvalListView.Of(List(eval)).Evals.Single();

        Assert.That(line.Modes.Single().Change, Is.Null);
        Assert.That(EvalListView.Of(List(Eval("cook", Run("run1", T0, (SampleMode.Alpha, 1.0))))).Evals.Single()
            .Modes.Single().Change, Is.Null);
    }

    /// <summary>An eval that never ran has no status, no time and no modes.</summary>
    [Test]
    public void AnEvalThatNeverRan_HasNoLastRun()
    {
        var line = EvalListView.Of(List(Eval("stub"))).Evals.Single();

        Assert.That((line.Status, line.LastAt, line.Modes.Count),
            Is.EqualTo(((string?)null, (DateTimeOffset?)null, 0)));
    }

    /// <summary>With no eval in the address, the one that ran last opens; with no runs at all, the first.</summary>
    [Test]
    public void TheDefaultEval_IsTheOneThatRanLast_ElseTheFirst()
    {
        var list = List(Eval("stub"), Eval("cook", Run("run1", T0.AddDays(-1))), Eval("route", Run("run2", T0)));

        Assert.That(EvalListView.DefaultEval(list), Is.EqualTo("route"));
        Assert.That(EvalListView.DefaultEval(List(Eval("stub"), Eval("cook"))), Is.EqualTo("stub"));
        Assert.That(EvalListView.DefaultEval(List()), Is.Null);
    }

    /// <summary>An eval's address names it, escaped.</summary>
    [Test]
    public void AnEvalsAddress_NamesIt() =>
        Assert.That(EvalAddress.Of("puls cook"), Is.EqualTo("admin?eval=puls%20cook"));
}
