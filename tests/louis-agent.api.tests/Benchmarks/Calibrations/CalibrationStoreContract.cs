using LouisAgent.Testing;
using System.Text.Json;
using LouisAgent.Api.Benchmarks.Calibrations;
using LouisAgent.Api.Benchmarks.Calibrations.Store;

namespace LouisAgent.Api.Tests.Benchmarks.Calibrations;

/// <summary>
/// F18 step 6a.5b: what every store of judge checks does, in memory and in MongoDB alike. A report is added, never changed; a run's reports come back newest first, so the card shows the
/// latest and folds the rest; deleting a run's reports says how many there were. Every test makes its own run ids, so they share a real database without seeing each other's reports.
/// </summary>
public abstract class CalibrationStoreContract
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 26, 9, 0, 0, TimeSpan.Zero);

    private ICalibrationStore _store = null!;

    protected abstract ICalibrationStore CreateStore();

    [SetUp]
    public void SetUpStore() => _store = CreateStore();

    private static string NewRun() => Guid.NewGuid().ToString("N")[..12];

    private static CalibrationRecord Report(string run, int seconds, int agreed = 35, string grader = "meal-for-people", string? judge = "qwen3:4b") =>
        new(run, grader, judge, T0.AddSeconds(seconds), agreed, 36, 90, agreed == 36, new CalibrationClassCounts(27, 0, 0), new CalibrationClassCounts(36 - agreed, 9 - (36 - agreed), 0),
            agreed == 36 ? [] : [new CalibrationDisagreement("cook-shops", SampleMode.Beta, 2, "fail", "pass", "a dish with steps")]);

    /// <summary>Records with a list inside compare by their JSON: a record's own equality compares the list by reference.</summary>
    private static string[] Json(IEnumerable<CalibrationRecord> records) => [.. records.Select(r => JsonSerializer.Serialize(r))];

    [Test]
    public async Task AReport_ReadsBackWithEverythingItWasGivenWith()
    {
        string run = NewRun();
        var report = Report(run, 1);

        await _store.AddAsync(report);

        Assert.That(Json(await _store.OfRunAsync(run)), Is.EqualTo(Json([report])));
    }

    [Test]
    public async Task AReportWithNoJudgeModelAndNoDisagreements_ReadsBackTheSame()
    {
        string run = NewRun();
        var report = Report(run, 1, agreed: 36, judge: null);

        await _store.AddAsync(report);

        Assert.That(Json(await _store.OfRunAsync(run)), Is.EqualTo(Json([report])));
    }

    [Test]
    public async Task ReportsComeBackNewestFirst_AndAnotherRunsAreNotAmongThem()
    {
        string run = NewRun(), other = NewRun();
        var first = Report(run, 1, agreed: 32);
        var second = Report(run, 60, agreed: 35);
        var third = Report(run, 120, agreed: 36);

        await _store.AddAsync(second);
        await _store.AddAsync(Report(other, 30));
        await _store.AddAsync(third);
        await _store.AddAsync(first);

        Assert.That(Json(await _store.OfRunAsync(run)), Is.EqualTo(Json([third, second, first])));
    }

    [Test]
    public async Task ARunWithNoReports_HasNone()
    {
        Assert.That(await _store.OfRunAsync(NewRun()), Is.Empty);
    }

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

/// <summary>The contract against the in-memory store: always, on every machine and on CI.</summary>
public class InMemoryCalibrationStoreTests : CalibrationStoreContract
{
    protected override ICalibrationStore CreateStore() => new InMemoryCalibrationStore();
}

/// <summary>The contract against MongoDB, when <c>BENCHMARKS_MONGO_URL</c> is set (as the review store's is): its own database, dropped when the fixture ends.</summary>
public class MongoCalibrationStoreTests : CalibrationStoreContract
{
    private static string? Url => Environment.GetEnvironmentVariable("BENCHMARKS_MONGO_URL");

    private const string Database = "puls_benchmarks_test_calibrations";

    [OneTimeSetUp]
    public void NeedsMongo() =>
        Assume.That(Url, Is.Not.Null.And.Not.Empty, "BENCHMARKS_MONGO_URL isn't set: the Mongo store isn't tested here");

    [OneTimeTearDown]
    public async Task DropTheTestDatabase()
    {
        if (string.IsNullOrEmpty(Url)) return;
        await new MongoCalibrationStore(Url, Database).Database.Client.DropDatabaseAsync(Database);
    }

    protected override ICalibrationStore CreateStore() => new MongoCalibrationStore(Url!, Database);
}
