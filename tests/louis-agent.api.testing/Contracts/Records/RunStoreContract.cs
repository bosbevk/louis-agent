using LouisAgent.Api.Evals.Runs.Kinds;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Api.Evals.Store.Runs;
using LouisAgent.Testing;
using static LouisAgent.Api.Evals.Store.EvalDocuments;

namespace LouisAgent.Api.Testing;

/// <summary>
/// What every store of runs does, in memory and in MongoDB alike. Saving a run again replaces it, the list comes back
/// newest first, and a restart closes the runs it cut off. Every test makes its own run ids and check name, so the
/// tests can share a real database without seeing each other's runs. Times are whole milliseconds in UTC, as MongoDB
/// keeps them.
/// </summary>
/// <remarks>
/// Abstract test (a contract test): the tests are written once, and each store gets a small subclass that only says
/// how to make one, so the in-memory store the unit tests use and the Mongo store can't drift apart.
/// </remarks>
public abstract class RunStoreContract
{
    /// <summary>The moment the tests start from.</summary>
    private static readonly DateTimeOffset T0 = new(2026, 10, 17, 9, 0, 0, TimeSpan.Zero);

    /// <summary>The store under test.</summary>
    private IRunStore _store = null!;

    /// <summary>This test's own check name, so its runs never show up in another test's list.</summary>
    private string _check = "";

    /// <summary>A store to test; each test gets a fresh one (a real store may share its database).</summary>
    protected abstract IRunStore CreateStore();

    /// <summary>A fresh store and a check name of its own before every test.</summary>
    [SetUp]
    public void SetUpStore()
    {
        _store = CreateStore();
        _check = "check-" + Guid.NewGuid().ToString("N")[..8];
    }

    /// <summary>A run id no other test uses, so tests sharing a database never meet.</summary>
    private static string NewId() => Guid.NewGuid().ToString("N")[..12];

    /// <summary>A run of this test's check with only its first fields, still going unless told otherwise.</summary>
    private Run NewRun(string id, DateTimeOffset startedAt, EvalRunStatus status = EvalRunStatus.Running) =>
        new(id, _check, 2, status, startedAt);

    /// <summary>
    /// The rates the runner wrote on a mode's line are kept on that mode; rates it left out, and a mode from before
    /// rates existed, stay null.
    /// </summary>
    [Test]
    public async Task AModesRates_ReadBackWithTheRun_AndAModeWithNoneReadsBackWithNone()
    {
        string id = NewId();
        var run = NewRun(id, T0) with
        {
            Modes =
            [
                new Mode(SampleMode.Beta, 1, [SampleToolset.Alpha], null, null, new ModeRates(1.0, 0.5, 2, 1, 1)),
                new Mode(SampleMode.Beta, 2, [SampleToolset.Alpha], null, null, new ModeRates(null, null, 1, 3, 0)),
                new Mode(SampleMode.Alpha, 1, [SampleToolset.Alpha], null, null),
            ],
        };

        await _store.SaveRunAsync(run);

        var read = (await _store.GetRunAsync(id))!.Modes!;
        Assert.That(read.Select(m => m.Rates),
            Is.EqualTo(new ModeRates?[] { new(1.0, 0.5, 2, 1, 1), new(null, null, 1, 3, 0), null }));
    }

    /// <summary>A finished run comes back with every field as it was written.</summary>
    [Test]
    public async Task ARun_ReadsBackAsItWasWritten()
    {
        string id = NewId();
        var run = NewRun(id, T0) with
        {
            Status = EvalRunStatus.Done,
            EndedAt = T0.AddMinutes(9),
            ExitCode = 0,
            Settings = new Dictionary<string, string>
            {
                ["model"] = "claude-haiku-5-5",
                ["commit"] = "45a98b3"
            },
            Modes = [new Mode(SampleMode.Beta, 1, [SampleToolset.Gamma, SampleToolset.Alpha], "puls/voice-af", null)],
            Total = new Total(0.4217m, "USD", 812_345, 61),
            Log = [SampleRole.Fill("<RoleAlpha> bobotie role=<RoleAlpha>"), "done"],
            StoreError = null,
        };

        await _store.SaveRunAsync(run);
        var read = await _store.GetRunAsync(id);

        Assert.That(read, Is.Not.Null);
        Assert.That((read!.Id, read.Check, read.Repeat, read.Status),
            Is.EqualTo((id, _check, 2, EvalRunStatus.Done)));
        Assert.That((read.StartedAt, read.EndedAt, read.ExitCode),
            Is.EqualTo((T0, (DateTimeOffset?)T0.AddMinutes(9), (int?)0)));
        Assert.That(read.Settings, Is.EquivalentTo(run.Settings));
        Assert.That(read.Modes!.Single() is { Name: SampleMode.Beta, Repeat: 1, Role: "puls/voice-af", Llm: null });
        Assert.That(read.Modes!.Single().Toolsets, Is.EqualTo(new[] { SampleToolset.Gamma, SampleToolset.Alpha }));
        Assert.That(read.Total, Is.EqualTo(new Total(0.4217m, "USD", 812_345, 61)));
        Assert.That(read.Log, Is.EqualTo(run.Log));
        Assert.That(read.StoreError, Is.Null);
    }

    /// <summary>Saving a run again replaces it rather than adding a second one.</summary>
    [Test]
    public async Task SavingARunAgain_ReplacesIt()
    {
        string id = NewId();
        await _store.SaveRunAsync(NewRun(id, T0));
        await _store.SaveRunAsync(NewRun(id, T0, EvalRunStatus.Failed) with
        {
            EndedAt = T0.AddMinutes(1),
            ExitCode = 2,
            StoreError = "store down"
        });

        var read = await _store.GetRunAsync(id);

        Assert.That((read!.Status, read.ExitCode, read.StoreError),
            Is.EqualTo((EvalRunStatus.Failed, (int?)2, "store down")));
        Assert.That((await _store.ListRunsAsync(_check)).Count, Is.EqualTo(1));
    }

    /// <summary>Asking for a run we don't have gives nothing back, not an error.</summary>
    [Test]
    public async Task AnUnknownRun_IsNull() =>
        Assert.That(await _store.GetRunAsync(NewId()), Is.Null);

    /// <summary>The list shows one check's runs, newest first, and stops at the limit.</summary>
    [Test]
    public async Task TheRuns_AreListedNewestFirst_ForOneCheck_UpToTheLimit()
    {
        // Three runs saved out of order, and one of another check.
        string first = NewId(), second = NewId(), third = NewId();
        await _store.SaveRunAsync(NewRun(second, T0.AddHours(1)));
        await _store.SaveRunAsync(NewRun(first, T0));
        await _store.SaveRunAsync(NewRun(third, T0.AddHours(2)));
        await _store.SaveRunAsync(NewRun(NewId(), T0.AddHours(3)) with { Check = _check + "-other" });

        var all = await _store.ListRunsAsync(_check);
        var two = await _store.ListRunsAsync(_check, limit: 2);

        Assert.That(all.Select(r => r.Id), Is.EqualTo(new[] { third, second, first }));
        Assert.That(two.Select(r => r.Id), Is.EqualTo(new[] { third, second }));
    }

    /// <summary>A run keeps which eval it ran, at what version and commit, with its graders and models.</summary>
    [Test]
    public async Task ARun_CarriesItsEvalGradersAndModels()
    {
        string id = NewId();
        var run = NewRun(id, T0) with
        {
            Eval = "cook",
            EvalVersion = 2,
            Commit = "abc123",
            Graders = [new Grader("priced-with-source", 1, GraderKind.FixedRule)],
            Models = [new ModelUse("cook", ModelLevel.Agent, SampleRole.Alpha, "claude-haiku-5-5")],
        };

        await _store.SaveRunAsync(run);
        var read = await _store.GetRunAsync(id);

        Assert.That((read!.Eval, read.EvalVersion, read.Commit), Is.EqualTo(("cook", (int?)2, "abc123")));
        Assert.That(read.Graders, Is.EqualTo(run.Graders));
        Assert.That(read.Models, Is.EqualTo(run.Models));
    }

    /// <summary>A run's split comes back as written, and a run from before splits has none.</summary>
    [TestCase(RunSplit.Dev)]
    [TestCase(RunSplit.HeldOut)]
    [TestCase(RunSplit.All)]
    [TestCase(null)]
    public async Task ARunsSplit_ReadsBackAsItWasWritten_AndARunBeforeItHasNone(RunSplit? split)
    {
        string id = NewId();

        await _store.SaveRunAsync(NewRun(id, T0) with { Split = split });

        Assert.That((await _store.GetRunAsync(id))!.Split, Is.EqualTo(split));
    }

    /// <summary>
    /// After a restart, runs still marked as going are closed as failed at that moment; finished runs are left alone.
    /// </summary>
    [Test]
    public async Task ClosingOrphans_FailsTheRunsStillRunning_AndLeavesTheFinishedOnes()
    {
        string running = NewId(), done = NewId();
        await _store.SaveRunAsync(NewRun(running, T0));
        await _store.SaveRunAsync(NewRun(done, T0, EvalRunStatus.Done) with
        {
            EndedAt = T0.AddMinutes(5),
            ExitCode = 0
        });

        int closed = await _store.CloseOrphansAsync(T0.AddHours(1));

        Assert.That(closed, Is.GreaterThanOrEqualTo(1));
        var orphan = await _store.GetRunAsync(running);
        Assert.That((orphan!.Status, orphan.EndedAt),
            Is.EqualTo((EvalRunStatus.Failed, (DateTimeOffset?)T0.AddHours(1))));
        var finished = await _store.GetRunAsync(done);
        Assert.That((finished!.Status, finished.EndedAt),
            Is.EqualTo((EvalRunStatus.Done, (DateTimeOffset?)T0.AddMinutes(5))));
    }

    /// <summary>
    /// The runs written under an older protocol version, or none, are listed; a run of this version is not. Other
    /// tests' runs may share the store, so it checks only this test's.
    /// </summary>
    [Test]
    public async Task TheRunsBeforeAVersion_AreListed_AndRunsOfThatVersionAreNot()
    {
        string current = NewId(), older = NewId(), none = NewId();
        await _store.SaveRunAsync(NewRun(current, T0) with { ProtocolVersion = 2 });
        await _store.SaveRunAsync(NewRun(older, T0) with { ProtocolVersion = 1 });
        await _store.SaveRunAsync(NewRun(none, T0));

        var listed = await _store.RunsBeforeAsync(2);

        Assert.That(listed, Does.Contain(older).And.Contain(none));
        Assert.That(listed, Does.Not.Contain(current));
    }

    /// <summary>Deleting a run takes only that run, and a second delete finds nothing.</summary>
    [Test]
    public async Task DeletingARun_TakesOnlyThatRun_AndSaysWhetherItWasThere()
    {
        string run = NewId(), other = NewId();
        await _store.SaveRunAsync(NewRun(run, T0));
        await _store.SaveRunAsync(NewRun(other, T0));

        Assert.That(await _store.DeleteAsync(run), Is.EqualTo(1));

        Assert.That(await _store.GetRunAsync(run), Is.Null);
        Assert.That(await _store.DeleteAsync(run), Is.Zero, "nothing left to delete");
        Assert.That(await _store.GetRunAsync(other), Is.Not.Null, "another run stays");
    }
}
