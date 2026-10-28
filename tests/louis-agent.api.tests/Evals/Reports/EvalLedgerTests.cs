using LouisAgent.Api.Evals.Reports;
using LouisAgent.Api.Evals.Runs;
using LouisAgent.Api.Evals.Store;
using LouisAgent.Api.Evals.Store.Recording;
using LouisAgent.Core.Usage.Records;
using LouisAgent.Core.Usage.Reports;
using LouisAgent.Core.Usage.Sinks;
using static LouisAgent.Api.Evals.Store.EvalDocuments;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Protocol.Events.Conversations;
using LouisAgent.Evals.Protocol.Events.Run;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Protocol.Parts.Conversations;
using LouisAgent.Evals.Protocol.Parts.Run;

namespace LouisAgent.Api.Tests.Evals.Reports;

/// <summary>
/// What a run cost comes from the usage ledger, never from the runner. The run's rows are the ones tagged with its
/// id, and each is placed on its turn: Puls's own by session and turn, a specialist's by its parent, a minion's
/// through the specialist that sent it. The sub-session roll-up rows are skipped, since they repeat what the
/// specialist's own rows already say and would count it twice.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0007-measure-every-request.md">ADR 0007: measure every request</seealso>
public class EvalLedgerTests
{
    /// <summary>The id of the run the rows belong to.</summary>
    private const string Run = "run123456789";

    /// <summary>When the first row was written.</summary>
    private static readonly DateTimeOffset T0 = new(2026, 10, 17, 9, 0, 0, TimeSpan.Zero);

    /// <summary>The bobotie conversation.</summary>
    private static readonly ConversationKey Bobotie = new(SampleMode.Beta, 1, "bobotie");

    /// <summary>The cottage pie conversation.</summary>
    private static readonly ConversationKey CottagePie = new(SampleMode.Beta, 1, "cottage-pie");

    /// <summary>The run's first line, as the runner writes it.</summary>
    private static string Begins() =>
        EvalProtocolJson.Line(new ProtocolEvent(T0, ProtocolEvent.CurrentVersion, "cook", 1, "abc", RunSplit.Dev,
            null, null, [], []));

    /// <summary>A ledger row for one request of this run, a cent by default.</summary>
    private static UsageRecord Row(int second, string? session, int? turn, string? run = Run + ":puls-cook:1",
        string? parent = null,
        string purpose = UsagePurpose.Turn, decimal? cost = 0.01m, int? requests = null, long input = 1000) =>
        new(T0.AddSeconds(second), 1, purpose, "api", session, turn, null, run, null, "claude-haiku-5-5",
            new UsageTokens(input, 0, 4000, 200, null), 1500, "stop",
            cost is null ? null : new UsageCost("USD", cost.Value, "2026-10-10"),
            parent, requests);

    /// <summary>Puls's two conversations, found by their sessions.</summary>
    private static readonly IReadOnlyDictionary<string, ConversationKey> Sessions =
        new Dictionary<string, ConversationKey>
        {
            ["sess_puls_bobotie"] = Bobotie,
            ["sess_puls_pie"] = CottagePie,
        };

    /// <summary>A ledger with every kind of row the run's join has to sort out.</summary>
    private static readonly UsageRecord[] Ledger =
    [
        Row(1, "sess_puls_bobotie", 1), // Puls, bobotie turn 1
        Row(2, "sess_cook", 1, parent: "sess_puls_bobotie#1"), // the cook it called
        Row(3, "sess_minion", 1, parent: "sess_cook#1", cost: 0.001m), // a minion the cook sent
        Row(4, "sess_puls_bobotie", 1, purpose: UsagePurpose.SubSession, cost: 0.011m,
            requests: 2), // the roll-up: skipped
        Row(5, "sess_puls_bobotie", 2), // bobotie turn 2
        Row(6, "sess_puls_pie", 1, cost: null), // a request with no price
        Row(7, "sess_elsewhere", 1, run: "run999999999:puls-cook:1"), // another run
        Row(8, "sess_elsewhere", 1, run: Run + "x:puls-cook:1"), // a run whose id starts with this one's
        Row(9, "sess_chat", 3, run: null), // somebody's chat meanwhile
        Row(10, "sess_lost", 1), // this run's, but no turn knows it
    ];

    /// <summary>The run's rows are the ones tagged with its id, leaving out the roll-ups.</summary>
    [Test]
    public void TheRunsRows_AreThoseTaggedWithItsId_WithoutTheRollUps()
    {
        var ofRun = EvalLedger.OfRun(Run, Ledger);

        Assert.That(ofRun.Select(r => r.At.Second), Is.EqualTo(new[] { 1, 2, 3, 5, 6, 10 }));
    }

    /// <summary>
    /// Each row lands on its turn through its session or its parent, keeping its tokens, cost and time; a row no turn
    /// knows is kept with no turn.
    /// </summary>
    [Test]
    public void EachRow_JoinsTheTurnItBelongsTo_ThroughItsSessionOrItsParent()
    {
        var requests = EvalLedger.Join(Run, EvalLedger.OfRun(Run, Ledger), Sessions);

        Assert.That(requests.Select(r => (r.At.Second, r.Turn)), Is.EqualTo(new (int, TurnKey?)[]
        {
            (1, new TurnKey(SampleMode.Beta, 1, "bobotie", 1)),
            (2, new TurnKey(SampleMode.Beta, 1, "bobotie", 1)), // the cook's, by its parent
            (3, new TurnKey(SampleMode.Beta, 1, "bobotie", 1)), // the minion's, through the cook
            (5, new TurnKey(SampleMode.Beta, 1, "bobotie", 2)), (6, new TurnKey(SampleMode.Beta, 1, "cottage-pie", 1)),
            (10, null), // kept and counted, but no turn to show it under
        }));
        var cook = requests[1];
        Assert.That((cook.Run, cook.Purpose, cook.Session, cook.Parent, cook.Model),
            Is.EqualTo((Run, UsagePurpose.Turn, "sess_cook", "sess_puls_bobotie#1", "claude-haiku-5-5")));
        Assert.That((cook.InputTokens, cook.CacheReadTokens, cook.OutputTokens, cook.CacheWriteTokens),
            Is.EqualTo(((long?)1000, (long?)4000, (long?)200, (long?)0)));
        Assert.That((cook.Cost, cook.DurationMs), Is.EqualTo(((decimal?)0.01m, 1500L)));
        Assert.That(requests[4].Cost, Is.Null, "an unpriced request stays unknown, never 0");
    }

    /// <summary>The run's total counts every request once.</summary>
    [Test]
    public void TheRunsTotal_CountsEveryRequestOnce()
    {
        var total = EvalLedger.TotalOf(EvalLedger.OfRun(Run, Ledger));

        // With the roll-up it would be 8 requests and $0.062: the cook's and the minion's requests twice.
        Assert.That(total, Is.EqualTo(new Total(0.041m, "USD", 6 * 5200, 6)));
    }

    /// <summary>The run's total is worked out the way the ledger's own report does it, so the two agree.</summary>
    [Test]
    public void TheTotal_IsUsageReportsTotal_SoThePortalAndTheLedgerAgree()
    {
        var ofRun = EvalLedger.OfRun(Run, Ledger);
        var report = UsageReport.Total(Run, ofRun);

        var total = EvalLedger.TotalOf(ofRun);

        Assert.That((total.Cost, total.Requests, total.Tokens),
            Is.EqualTo((report.Cost, report.Requests,
                report.Input + report.CacheWrite + report.CacheRead + report.Output)));
    }

    /// <summary>A run where nothing is priced has an unknown cost, not zero.</summary>
    [Test]
    public void ARunWithNoPricedRequest_HasAnUnknownCost_NotZero()
    {
        var total = EvalLedger.TotalOf([Row(1, "sess_puls_bobotie", 1, cost: null)]);

        Assert.That(total.Cost, Is.Null);
    }

    /// <summary>
    /// A run that crosses a month end reads both months' ledger files and no others; no ledger at all is no rows.
    /// </summary>
    [Test]
    public void TheLedger_IsReadOverEveryMonthTheRunSpans()
    {
        string directory = Path.Combine(Path.GetTempPath(), "louis-ledger-" + Guid.NewGuid().ToString("N"));
        // The folder is ours alone, and the finally removes it whatever the test does.
        try
        {
            // Two rows a minute either side of September's end, and one from August.
            var sink = new JsonlUsageSink(directory);
            var september = new DateTimeOffset(2026, 9, 30, 23, 59, 0, TimeSpan.Zero);
            sink.Record(Row(0, "sess_puls_bobotie", 1) with { At = september });
            sink.Record(Row(0, "sess_puls_bobotie", 2) with { At = september.AddMinutes(2) });
            sink.Record(Row(0, "sess_old", 1) with { At = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero) });

            var read = EvalLedger.Read(directory, september.AddMinutes(-1), september.AddMinutes(3)).ToList();
            var none = EvalLedger.Read(Path.Combine(directory, "missing"), september, september).ToList();

            Assert.That(read.Select(r => r.Turn), Is.EqualTo(new int?[] { 1, 2 }),
                "September's and October's files, not August's");
            Assert.That(none, Is.Empty, "no ledger yet is no rows, not an error");
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>The run's prompt hashes are listed once each, sorted, skipping rows that have none.</summary>
    [Test]
    public void PromptHashesOf_AreTheDistinctOnesSorted_AndRowsWithNoneAreSkipped()
    {
        UsageRecord[] rows =
        [
            Row(1, "s", 1) with { PromptHash = "b" }, Row(2, "s", 1) with { PromptHash = "a" },
            Row(3, "s", 1) with { PromptHash = "b" }, Row(4, "s", 1)
        ];

        Assert.That(EvalLedger.PromptHashesOf(rows), Is.EqualTo(new[] { "a", "b" }));
        Assert.That(EvalLedger.PromptHashesOf([Row(1, "s", 1)]), Is.Empty);
    }

    /// <summary>
    /// Records a run with these settings over a ledger with one row per prompt hash, and hands back what the store
    /// kept.
    /// </summary>
    private static async Task<Run> RecordedWith(IReadOnlyDictionary<string, string> settings, params string?[] hashes)
    {
        string directory = Path.Combine(Path.GetTempPath(), "louis-ledger-" + Guid.NewGuid().ToString("N"));
        // The folder is ours alone, and the finally removes it whatever the test does.
        try
        {
            var store = new InMemoryEvalStore();
            var run = new RunningEval(Run, "cook", 1);
            var sink = new JsonlUsageSink(directory);
            foreach (string? hash in hashes)
                sink.Record(Row(0, "sess_puls_bobotie", 1) with
                {
                    At = DateTimeOffset.UtcNow,
                    PromptHash = hash
                });

            // A run of only its begin line; when it ends, the recorder reads the ledger.
            var recording = new EvalRecorder(store, ledgerDirectory: directory).RecordAsync(run, settings);
            run.Add(Begins());
            run.Complete(0);
            await recording;

            return (await store.GetRunAsync(Run))!;
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>A run whose rows carry two prompt hashes keeps them sorted beside its own settings.</summary>
    [Test]
    public async Task ARunWhoseRowsCarryTwoHashes_IsStoredWithThemSorted_BesideItsOwnSettings()
    {
        var stored = await RecordedWith(new Dictionary<string, string> { ["model"] = "m" }, "b", "a", "b");

        Assert.That(stored.Settings,
            Is.EquivalentTo(new Dictionary<string, string>
            {
                ["model"] = "m",
                ["prompt_hashes"] = "a,b"
            }));
    }

    /// <summary>A run with no prompt hashes gets no hash setting, and its total is the same.</summary>
    [Test]
    public async Task ARunWhoseLedgerHasNoHashes_IsStoredWithoutTheKey_AndTheSameTotal()
    {
        var stored = await RecordedWith(new Dictionary<string, string> { ["model"] = "m" }, null, null);

        Assert.That(stored.Settings, Is.EquivalentTo(new Dictionary<string, string> { ["model"] = "m" }));
        Assert.That(stored.Total, Is.EqualTo(new Total(0.02m, "USD", 2 * 5200, 2)));
    }

    /// <summary>
    /// When a run ends, the recorder saves its requests on their turns and its total, leaving the roll-up out.
    /// </summary>
    [Test]
    public async Task WhenARunEnds_TheRecorderWritesItsRequestsAndTotal()
    {
        string directory = Path.Combine(Path.GetTempPath(), "louis-ledger-" + Guid.NewGuid().ToString("N"));
        // The folder is ours alone, and the finally removes it whatever the test does.
        try
        {
            // A ledger with Puls's turn, the cook it called, and the roll-up of the cook.
            var store = new InMemoryEvalStore();
            var run = new RunningEval(Run, "cook", 1);
            var sink = new JsonlUsageSink(directory);
            var now = DateTimeOffset.UtcNow;
            sink.Record(Row(0, "sess_puls_bobotie", 1) with { At = now });
            sink.Record(Row(0, "sess_cook", 1, parent: "sess_puls_bobotie#1") with { At = now });
            sink.Record(Row(0, "sess_puls_bobotie", 1, purpose: UsagePurpose.SubSession, requests: 1) with
            {
                At = now
            });

            // A run of one question and its turn on that session; when it ends, the recorder reads the ledger.
            var recording = new EvalRecorder(store, ledgerDirectory: directory).RecordAsync(run);

            run.Add(Begins());
            run.Add(EvalProtocolJson.Line(new QuestionEvent(now, SampleMode.Beta, 1, "bobotie", 1, "sess_puls_bobotie",
                "?")));
            run.Add(EvalProtocolJson.Line(new TurnEvent(now, SampleMode.Beta, 1, "bobotie", 1, "sess_puls_bobotie", "?",
                null, null, null, 0, 0, [], 0, null, null)));
            run.Complete(0);
            await recording;

            // Both requests sit on the turn, the roll-up isn't one, and the total is the two.
            var requests = await store.RequestsAsync(Run);
            Assert.That(requests.Select(r => r.Turn), Is.All.EqualTo(new TurnKey(SampleMode.Beta, 1, "bobotie", 1)));
            Assert.That(requests.Count, Is.EqualTo(2), "the roll-up isn't a request of its own");
            Assert.That((await store.GetRunAsync(Run))!.Total, Is.EqualTo(new Total(0.02m, "USD", 2 * 5200, 2)));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }
}
