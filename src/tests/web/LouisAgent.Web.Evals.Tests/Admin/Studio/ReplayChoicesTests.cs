using LouisAgent.Api.Contracts.Admin.Evals;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Web.Evals.Admin.Studio;

namespace LouisAgent.Web.Evals.Tests.Admin.Studio;

/// <summary>
/// The runs a try can replay (the admin review's finding 6, F53's leftover): picked from a list instead of an id typed
/// from memory. Only a run that recorded its answers can be replayed; the draft's own eval comes first, newest first.
/// </summary>
public class ReplayChoicesTests
{
    /// <summary>A stored run of an eval, recorded or not, started at the hour given.</summary>
    private static EvalRunRow Run(string id, int hour, bool recorded) =>
        new(new StoredRunRow(id, "check", 1, "completed", 0, new DateTimeOffset(2026, 11, 4, hour, 0, 0, TimeSpan.Zero),
            null, null, null, null, null, null, null, null, null, Recorded: recorded), []);

    /// <summary>An eval with these runs.</summary>
    private static EvalRow Eval(string name, params EvalRunRow[] runs) =>
        new(name, "weather-agent", 1, "", false, null, [], [], [], [], false, runs);

    [Test]
    public void OnlyRecordedRuns_AreOffered_TheDraftsOwnEvalFirst_NewestFirst()
    {
        var evals = new[]
        {
            Eval("eval-beta", Run("r-old", 8, recorded: true), Run("r-new", 12, recorded: true)),
            Eval("eval-alpha", Run("c-old", 9, recorded: true), Run("c-live", 13, recorded: false),
                Run("c-new", 11, recorded: true)),
        };

        var choices = ReplayChoices.From(evals, "eval-alpha");

        Assert.That(choices.Select(c => c.Run), Is.EqualTo(new[] { "c-new", "c-old", "r-new", "r-old" }));
    }

    [Test]
    public void AChoice_NamesItsEval_ItsStart_InUtc_AndItsId()
    {
        var choice = ReplayChoices.From([Eval("eval-alpha", Run("c0362ff73151", 9, recorded: true))], null).Single();

        Assert.That(choice.Label, Is.EqualTo("eval-alpha · 2026-11-04 09:00 · c0362ff73151"));
    }

    [Test]
    public void NoRecordedRun_IsNoChoice() =>
        Assert.That(ReplayChoices.From([Eval("eval-alpha", Run("c1", 9, recorded: false))], "eval-alpha"), Is.Empty);
}
