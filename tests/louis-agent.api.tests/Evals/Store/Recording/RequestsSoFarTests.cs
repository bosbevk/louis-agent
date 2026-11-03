using LouisAgent.Api.Evals.Runs;
using LouisAgent.Api.Evals.Store;
using LouisAgent.Api.Evals.Store.Recording;
using LouisAgent.Core.Usage.Records;
using LouisAgent.Core.Usage.Sinks;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Protocol.Events.Conversations;
using LouisAgent.Evals.Protocol.Events.Run;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Protocol.Parts.Conversations;

namespace LouisAgent.Api.Tests.Evals.Store.Recording;

/// <summary>
/// While a run goes on, the recorder can say what its requests are so far (FX34): the ledger's rows of the run, each
/// on its turn through the sessions the recording heard, the same join the run's end makes. So the live table can
/// show a turn's cost as it ends, not only when the whole run has.
/// </summary>
public class RequestsSoFarTests
{
    /// <summary>The run being recorded.</summary>
    private const string RunId = "run123456789";

    /// <summary>The turn the requests belong to.</summary>
    private static readonly TurnKey Bobotie = new(SampleMode.Beta, 1, "bobotie", 1);

    /// <summary>A fresh ledger folder for each test.</summary>
    private string _ledger = null!;

    /// <summary>The run, fed by hand.</summary>
    private RunningEval _run = null!;

    /// <summary>Gives each test an empty ledger folder and a fresh run.</summary>
    [SetUp]
    public void SetUp()
    {
        _ledger = Path.Combine(Path.GetTempPath(), "louis-usage-ledger-" + Guid.NewGuid().ToString("N"));
        _run = new RunningEval(RunId, "cook", 1);
    }

    /// <summary>Removes the ledger folder.</summary>
    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_ledger)) Directory.Delete(_ledger, recursive: true);
    }

    /// <summary>
    /// A turn asked and answered, with no end of the run yet: its two requests are there, on that turn, with their
    /// costs.
    /// </summary>
    [Test]
    public async Task ATurnsRequests_AreThere_BeforeTheRunEnds()
    {
        // Two requests of the turn in the ledger, as the API's session wrote them.
        Asked(0.25m);
        Asked(0.05m);

        // The run is recorded and has asked its first question; it hasn't ended.
        var store = new InMemoryEvalStore();
        var recorder = new EvalRecorder(store, ledgerDirectory: _ledger);
        recorder.Attach(_run);
        foreach (string line in FirstTurn()) _run.Add(line);
        await Background.Eventually(async () => (await store.TurnsAsync(RunId, Bobotie.Conversation)).Count == 1);

        var requests = recorder.RequestsSoFar(RunId);

        Assert.That(requests, Is.Not.Null);
        Assert.That(requests!.Select(r => (r.Turn, r.Cost)),
            Is.EqualTo(new[] { ((TurnKey?)Bobotie, (decimal?)0.25m), (Bobotie, 0.05m) }));
    }

    /// <summary>A run this recorder isn't recording has nothing to say.</summary>
    [Test]
    public void ARunNotRecordedHere_HasNoRequests() =>
        Assert.That(new EvalRecorder(new InMemoryEvalStore(), ledgerDirectory: _ledger).RequestsSoFar("other"),
            Is.Null);

    /// <summary>Without a ledger a recorded run has no requests yet, never an error.</summary>
    [Test]
    public void NoLedger_GivesNoRequests()
    {
        var recorder = new EvalRecorder(new InMemoryEvalStore());
        recorder.Attach(_run);

        Assert.That(recorder.RequestsSoFar(RunId), Is.Empty);
    }

    /// <summary>
    /// A ledger that can't be read just now (a line half written) says so, instead of an empty list that would blank
    /// the costs the page already shows.
    /// </summary>
    [Test]
    public void ALedgerThatCantBeRead_SaysSo()
    {
        // One whole row, then the start of the next one, as a read in the middle of a write would find the file.
        Asked(0.25m);
        File.AppendAllText(Directory.GetFiles(_ledger).Single(), "{\"at\":");
        var recorder = new EvalRecorder(new InMemoryEvalStore(), ledgerDirectory: _ledger);
        recorder.Attach(_run);

        Assert.That(() => recorder.RequestsSoFar(RunId), Throws.TypeOf<InvalidDataException>());
    }

    /// <summary>Writes one request of bobotie's turn to the ledger, at this cost.</summary>
    private void Asked(decimal cost) =>
        new JsonlUsageSink(_ledger).Record(new UsageRecord(DateTimeOffset.UtcNow, 1, UsagePurpose.Turn, "api",
            "sess_1", 1, "bobotie", $"{RunId}:{SampleMode.Beta}:1", null, "claude-haiku-5-5",
            new UsageTokens(1000, 0, 0, 200, null), 1500, "stop", new UsageCost("USD", cost, "2026-10-10"), null,
            null));

    /// <summary>The lines of a run up to its first turn's end, in session <c>sess_1</c>.</summary>
    private static IEnumerable<string> FirstTurn()
    {
        var at = DateTimeOffset.UtcNow;
        EvalEvent[] events =
        [
            new ProtocolEvent(at, ProtocolEvent.CurrentVersion, "cook", 1, "abc", RunSplit.Dev, null, null, [], []),
            new ModeEvent(at, SampleMode.Beta, 1, [], null, null, 1),
            new QuestionEvent(at, SampleMode.Beta, 1, "bobotie", 1, "sess_1", "Maak bobotie"),
            new TurnEvent(at, SampleMode.Beta, 1, "bobotie", 1, "sess_1", "Maak bobotie", "Hier", null, null, 1, 0, [],
                2.0, null, null),
        ];
        return events.Select(EvalProtocolJson.Line);
    }
}
