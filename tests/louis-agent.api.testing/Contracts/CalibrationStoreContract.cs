using LouisAgent.Testing;
using System.Text.Json;
using LouisAgent.Api.Benchmarks.Calibrations;
using LouisAgent.Api.Benchmarks.Calibrations.Store;

namespace LouisAgent.Api.Testing;

/// <summary>
/// What every store of judge checks does, in memory and in MongoDB alike. A report is added, never changed; a run's
/// reports come back newest first, so the card shows the latest and folds the rest away; deleting a run's reports says
/// how many there were. Every test makes its own run ids, so tests can share a real database without seeing each
/// other's reports.
/// </summary>
/// <remarks>
/// Abstract test (a contract test): the tests are written once, and each store gets a small subclass that only says
/// how to make one, so the in-memory store and the Mongo store are held to the same rules.
/// </remarks>
public abstract class CalibrationStoreContract
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 26, 9, 0, 0, TimeSpan.Zero);

    private ICalibrationStore _store = null!;

    /// <summary>A store to test; each test gets a fresh one (a real store may share its database).</summary>
    protected abstract ICalibrationStore CreateStore();

    /// <summary>A fresh store before every test.</summary>
    [SetUp]
    public void SetUpStore() => _store = CreateStore();

    /// <summary>A run id no other test uses, so tests sharing a database never meet.</summary>
    private static string NewRun() => Guid.NewGuid().ToString("N")[..12];

    /// <summary>
    /// A judge check's report on a run, some seconds after the start: by default 35 of 36 agreed, with one
    /// disagreement. When all 36 agree it passes and lists no disagreements.
    /// </summary>
    private static CalibrationRecord Report(string run, int seconds, int agreed = 35, string grader = "meal-for-people",
        string? judge = "qwen3:4b") =>
        new(run, grader, judge, T0.AddSeconds(seconds), agreed, 36, 90, agreed == 36,
            new CalibrationClassCounts(27, 0, 0), new CalibrationClassCounts(36 - agreed, 9 - (36 - agreed), 0),
            agreed == 36
                ? []
                : [new CalibrationDisagreement("cook-shops", SampleMode.Beta, 2, "fail", "pass", "a dish with steps")]);

    /// <summary>
    /// Turns reports into JSON to compare them, since a record's own equality compares its list by reference.
    /// </summary>
    private static string[] Json(IEnumerable<CalibrationRecord> records) =>
        [.. records.Select(r => JsonSerializer.Serialize(r))];

    /// <summary>A report comes back with everything it was stored with.</summary>
    [Test]
    public async Task AReport_ReadsBackWithEverythingItWasGivenWith()
    {
        string run = NewRun();
        var report = Report(run, 1);

        await _store.AddAsync(report);

        Assert.That(Json(await _store.OfRunAsync(run)), Is.EqualTo(Json([report])));
    }

    /// <summary>A report with no judge model and no disagreements comes back just the same.</summary>
    [Test]
    public async Task AReportWithNoJudgeModelAndNoDisagreements_ReadsBackTheSame()
    {
        string run = NewRun();
        var report = Report(run, 1, agreed: 36, judge: null);

        await _store.AddAsync(report);

        Assert.That(Json(await _store.OfRunAsync(run)), Is.EqualTo(Json([report])));
    }

    /// <summary>A run's reports come back newest first, and another run's never show up among them.</summary>
    [Test]
    public async Task ReportsComeBackNewestFirst_AndAnotherRunsAreNotAmongThem()
    {
        string run = NewRun(), other = NewRun();
        var first = Report(run, 1, agreed: 32);
        var second = Report(run, 60, agreed: 35);
        var third = Report(run, 120, agreed: 36);

        // Added out of order, with another run's report in between.
        await _store.AddAsync(second);
        await _store.AddAsync(Report(other, 30));
        await _store.AddAsync(third);
        await _store.AddAsync(first);

        Assert.That(Json(await _store.OfRunAsync(run)), Is.EqualTo(Json([third, second, first])));
    }

    /// <summary>A run that was never checked has no reports, rather than an error.</summary>
    [Test]
    public async Task ARunWithNoReports_HasNone()
    {
        Assert.That(await _store.OfRunAsync(NewRun()), Is.Empty);
    }

    /// <summary>
    /// Deleting a run's reports removes only that run's and says how many went; doing it again removes none.
    /// </summary>
    [Test]
    public async Task DeletingARun_RemovesItsReports_AndSaysHowMany()
    {
        string run = NewRun(), other = NewRun();
        await _store.AddAsync(Report(run, 1));
        await _store.AddAsync(Report(run, 2));
        await _store.AddAsync(Report(other, 1));

        Assert.That(await _store.DeleteRunAsync(run), Is.EqualTo(2));
        Assert.That(await _store.OfRunAsync(run), Is.Empty);
        Assert.That(await _store.OfRunAsync(other), Has.Count.EqualTo(1));
        Assert.That(await _store.DeleteRunAsync(run), Is.Zero);
    }
}
