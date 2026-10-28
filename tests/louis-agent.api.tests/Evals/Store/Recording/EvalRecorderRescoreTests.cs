using System.Text.Json;
using LouisAgent.Api.Evals.Runs;
using LouisAgent.Api.Evals.Store;
using LouisAgent.Api.Evals.Store.Recording;
using static LouisAgent.Api.Evals.Store.EvalDocuments;

namespace LouisAgent.Api.Tests.Evals.Store.Recording;

/// <summary>
/// What the recorder writes for a re-score. A re-score asks the model nothing, so it costs a known zero, unless a
/// judge was asked, and then it costs that judge's own rows. These tests moved out of <c>BenchmarkRecorderTests</c>
/// to keep it under the test-file size limit.
/// </summary>
public class EvalRecorderRescoreTests
{
    /// <summary>The store the recorder writes into.</summary>
    private InMemoryEvalStore _store = null!;

    /// <summary>Gives each test a fresh store.</summary>
    [SetUp]
    public void SetUp()
    {
        _store = new InMemoryEvalStore();
    }

    /// <summary>Turns an event into one of the runner's protocol lines: an <c>@</c> and the event as JSON.</summary>
    private static string At(object e) => "@" + JsonSerializer.Serialize(e);

    /// <summary>
    /// A re-score asks the model nothing, so its cost is a known zero (not "unknown"), whatever the ledger holds for
    /// the original run.
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public async Task ARescoresTotal_IsZeroRequestsAndZeroCost_WhateverTheLedgerHolds(bool ledgerHoldsTheOriginalsRows)
    {
        // A ledger folder of our own, deleted at the end.
        string directory = Path.Combine(Path.GetTempPath(), "louis-rescore-ledger-" + Guid.NewGuid().ToString("N"));
        try
        {
            // The ledger may hold the original run's rows; the re-score must not count them.
            if (ledgerHoldsTheOriginalsRows)
                new LouisAgent.Core.Usage.Sinks.JsonlUsageSink(directory).Record(
                    new LouisAgent.Core.Usage.Records.UsageRecord(
                        DateTimeOffset.UtcNow, 1, LouisAgent.Core.Usage.Records.UsagePurpose.Turn, "api", "sess_old", 1,
                        null, "old123456789:cook:1", null, "claude-haiku-5-5",
                        new LouisAgent.Core.Usage.Records.UsageTokens(1000, 0, 0, 200, null), 1500, "stop",
                        new LouisAgent.Core.Usage.Records.UsageCost("USD", 0.5m, "2026-10-10"), null, null));

            // A re-score of the old run, recorded against that ledger.
            var run = new RunningEval("new123456789", "cook", 1, rescoreOf: "old123456789");
            var recording = new EvalRecorder(_store, ledgerDirectory: directory).RecordAsync(run);

            // Its first line says it re-scores the old run, then the run ends.
            run.Add(At(new
            {
                @event = "protocol",
                version = 1,
                eval = "cook",
                eval_version = 1,
                rescore_of = "old123456789",
                graders = Array.Empty<object>(),
                models = Array.Empty<object>()
            }));
            run.Complete(0);
            await recording;

            var stored = (await _store.GetRunAsync("new123456789"))!;
            Assert.That(stored.Total, Is.EqualTo(new Total(0m, "USD", 0, 0)));
            Assert.That(stored.RescoreOf, Is.EqualTo("old123456789"));
            Assert.That(await _store.RequestsAsync("new123456789"), Is.Empty);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// A re-score is free only until a grader asks a paid model: then the judge's own ledger rows are its cost, like
    /// any run's.
    /// </summary>
    [Test]
    public async Task AReScoreThatAskedAJudge_IsPricedFromItsOwnRows_NotShownAsFree()
    {
        // A ledger folder of our own, deleted at the end.
        string directory = Path.Combine(Path.GetTempPath(), "louis-rescore-judge-" + Guid.NewGuid().ToString("N"));
        try
        {
            // The judge asked a paid model, so the ledger has a row under the re-score's own tag.
            new LouisAgent.Core.Usage.Sinks.JsonlUsageSink(directory).Record(
                new LouisAgent.Core.Usage.Records.UsageRecord(
                    DateTimeOffset.UtcNow, 1, LouisAgent.Core.Usage.Records.UsagePurpose.Grading, "api", "sess_1", 1,
                    "bobotie", "new123456789:cook:1", null, "claude-haiku-5-5",
                    new LouisAgent.Core.Usage.Records.UsageTokens(1000, 0, 0, 200, null), 1500, "stop",
                    new LouisAgent.Core.Usage.Records.UsageCost("USD", 0.25m, "2026-10-10"), null, null));

            // A re-score of the old run with one question, recorded against that ledger.
            var run = new RunningEval("new123456789", "cook", 1, rescoreOf: "old123456789");
            var recording = new EvalRecorder(_store, ledgerDirectory: directory).RecordAsync(run);

            // Its first line says it re-scores the old run; then the question; then the run ends.
            run.Add(At(new
            {
                @event = "protocol",
                version = 1,
                eval = "cook",
                eval_version = 1,
                rescore_of = "old123456789",
                graders = Array.Empty<object>(),
                models = Array.Empty<object>()
            }));
            run.Add(At(new
            {
                @event = "question",
                mode = "cook",
                repeat = 1,
                task = "bobotie",
                turn = 1,
                session = "sess_1",
                text = "Maak bobotie"
            }));
            run.Complete(0);
            await recording;

            var stored = (await _store.GetRunAsync("new123456789"))!;
            Assert.That(stored.Total, Is.EqualTo(new Total(0.25m, "USD", 1200, 1)));
            Assert.That((await _store.RequestsAsync("new123456789")).Single().Purpose, Is.EqualTo("grading"));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>A re-score with no ledger at all still costs a known zero.</summary>
    [Test]
    public async Task ARescoreWithNoLedgerAtAll_StillCostsAKnownZero()
    {
        // A re-score recorded with no ledger folder at all.
        var run = new RunningEval("new123456789", "cook", 1, rescoreOf: "old123456789");
        var recording = new EvalRecorder(_store).RecordAsync(run);

        run.Complete(0);
        await recording;

        Assert.That((await _store.GetRunAsync("new123456789"))!.Total, Is.EqualTo(new Total(0m, "USD", 0, 0)));
    }
}
