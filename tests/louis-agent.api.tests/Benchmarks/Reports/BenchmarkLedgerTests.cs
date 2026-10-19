using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using LouisAgent.Api.Benchmarks.Reports;
using LouisAgent.Core.Usage.Records;
using LouisAgent.Core.Usage.Sinks;
using LouisAgent.Core.Usage.Reports;
using System.Text.Json;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;
using static LouisAgent.Api.Benchmarks.Runs.BenchmarkProtocol;

namespace LouisAgent.Api.Tests.Benchmarks.Reports;

// F31 step 4: what a run cost comes from the usage ledger, never from the script. The run's rows are those tagged with
// its id; each is joined to the turn it belongs to: Puls's own by its session and turn, a specialist's by its parent
// (`{session}#{turn}`), a minion's through the specialist that sent it. The sub-session roll-up rows are skipped:
// they repeat, in Puls's turn, what the specialist's own rows already say, and would count it twice.
public class BenchmarkLedgerTests
{
    private const string Run = "run123456789";
    private static readonly DateTimeOffset T0 = new(2026, 10, 17, 9, 0, 0, TimeSpan.Zero);
    private static readonly ConversationKey Bobotie = new("puls-cook", 1, "bobotie");
    private static readonly ConversationKey CottagePie = new("puls-cook", 1, "cottage-pie");

    private static UsageRecord Row(int second, string? session, int? turn, string? run = Run + ":puls-cook:1", string? parent = null,
        string purpose = UsagePurpose.Turn, decimal? cost = 0.01m, int? requests = null, long input = 1000) =>
        new(T0.AddSeconds(second), 1, purpose, "api", session, turn, null, run, null, "claude-haiku-5-5",
            new UsageTokens(input, 0, 4000, 200, null), 1500, "stop", cost is null ? null : new UsageCost("USD", cost.Value, "2026-10-10"),
            parent, requests);

    private static readonly IReadOnlyDictionary<string, ConversationKey> Sessions = new Dictionary<string, ConversationKey>
    {
        ["sess_puls_bobotie"] = Bobotie,
        ["sess_puls_pie"] = CottagePie,
    };

    private static readonly UsageRecord[] Ledger =
    [
        Row(1, "sess_puls_bobotie", 1),                                                           // Puls, bobotie turn 1
        Row(2, "sess_cook", 1, parent: "sess_puls_bobotie#1"),                                    // the cook it called
        Row(3, "sess_minion", 1, parent: "sess_cook#1", cost: 0.001m),                            // a minion the cook sent
        Row(4, "sess_puls_bobotie", 1, purpose: UsagePurpose.SubSession, cost: 0.011m, requests: 2), // the roll-up: skipped
        Row(5, "sess_puls_bobotie", 2),                                                           // bobotie turn 2
        Row(6, "sess_puls_pie", 1, cost: null),                                                   // a request with no price
        Row(7, "sess_elsewhere", 1, run: "run999999999:puls-cook:1"),                            // another run
        Row(8, "sess_elsewhere", 1, run: Run + "x:puls-cook:1"),                                 // a run whose id starts with this one's
        Row(9, "sess_chat", 3, run: null),                                                        // somebody's chat meanwhile
        Row(10, "sess_lost", 1),                                                                  // this run's, but no turn knows it
    ];

    [Test]
    public void TheRunsRows_AreThoseTaggedWithItsId_WithoutTheRollUps()
    {
        var ofRun = BenchmarkLedger.OfRun(Run, Ledger);

        Assert.That(ofRun.Select(r => r.At.Second), Is.EqualTo(new[] { 1, 2, 3, 5, 6, 10 }));
    }

    [Test]
    public void EachRow_JoinsTheTurnItBelongsTo_ThroughItsSessionOrItsParent()
    {
        var requests = BenchmarkLedger.Join(Run, BenchmarkLedger.OfRun(Run, Ledger), Sessions);

        Assert.That(requests.Select(r => (r.At.Second, r.Turn)), Is.EqualTo(new (int, TurnKey?)[]
        {
            (1, new TurnKey("puls-cook", 1, "bobotie", 1)),
            (2, new TurnKey("puls-cook", 1, "bobotie", 1)),      // the cook's, by its parent
            (3, new TurnKey("puls-cook", 1, "bobotie", 1)),      // the minion's, through the cook
            (5, new TurnKey("puls-cook", 1, "bobotie", 2)),
            (6, new TurnKey("puls-cook", 1, "cottage-pie", 1)),
            (10, null),                                           // kept and counted, but no turn to show it under
        }));
        var cook = requests[1];
        Assert.That((cook.Run, cook.Purpose, cook.Session, cook.Parent, cook.Model), Is.EqualTo((Run, UsagePurpose.Turn, "sess_cook", "sess_puls_bobotie#1", "claude-haiku-5-5")));
        Assert.That((cook.InputTokens, cook.CacheReadTokens, cook.OutputTokens, cook.CacheWriteTokens), Is.EqualTo(((long?)1000, (long?)4000, (long?)200, (long?)0)));
        Assert.That((cook.Cost, cook.DurationMs), Is.EqualTo(((decimal?)0.01m, 1500L)));
        Assert.That(requests[4].Cost, Is.Null, "an unpriced request stays unknown, never 0");
    }

    [Test]
    public void TheRunsTotal_CountsEveryRequestOnce()
    {
        var total = BenchmarkLedger.TotalOf(BenchmarkLedger.OfRun(Run, Ledger));

        // With the roll-up it would be 8 requests and $0.062: the cook's and the minion's requests twice.
        Assert.That(total, Is.EqualTo(new Total(0.041m, "USD", 6 * 5200, 6)));
    }

    [Test]
    public void TheTotal_IsUsageReportsTotal_SoThePortalAndTheLedgerAgree()
    {
        var ofRun = BenchmarkLedger.OfRun(Run, Ledger);
        var report = UsageReport.Total(Run, ofRun);

        var total = BenchmarkLedger.TotalOf(ofRun);

        Assert.That((total.Cost, total.Requests, total.Tokens), Is.EqualTo((report.Cost, report.Requests, report.Input + report.CacheWrite + report.CacheRead + report.Output)));
    }

    [Test]
    public void ARunWithNoPricedRequest_HasAnUnknownCost_NotZero()
    {
        var total = BenchmarkLedger.TotalOf([Row(1, "sess_puls_bobotie", 1, cost: null)]);

        Assert.That(total.Cost, Is.Null);
    }

    [Test]
    public void TheLedger_IsReadOverEveryMonthTheRunSpans()
    {
        string directory = Path.Combine(Path.GetTempPath(), "louis-ledger-" + Guid.NewGuid().ToString("N"));
        try
        {
            var sink = new JsonlUsageSink(directory);
            var september = new DateTimeOffset(2026, 9, 30, 23, 59, 0, TimeSpan.Zero);
            sink.Record(Row(0, "sess_puls_bobotie", 1) with { At = september });
            sink.Record(Row(0, "sess_puls_bobotie", 2) with { At = september.AddMinutes(2) });
            sink.Record(Row(0, "sess_old", 1) with { At = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero) });

            var read = BenchmarkLedger.Read(directory, september.AddMinutes(-1), september.AddMinutes(3)).ToList();
            var none = BenchmarkLedger.Read(Path.Combine(directory, "missing"), september, september).ToList();

            Assert.That(read.Select(r => r.Turn), Is.EqualTo(new int?[] { 1, 2 }), "September's and October's files, not August's");
            Assert.That(none, Is.Empty, "no ledger yet is no rows, not an error");
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Test]
    public async Task WhenARunEnds_TheRecorderWritesItsRequestsAndTotal()
    {
        string directory = Path.Combine(Path.GetTempPath(), "louis-ledger-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new InMemoryBenchmarkStore();
            var run = new BenchmarkRun(Run, "cook", 1);
            var sink = new JsonlUsageSink(directory);
            var now = DateTimeOffset.UtcNow;
            sink.Record(Row(0, "sess_puls_bobotie", 1) with { At = now });
            sink.Record(Row(0, "sess_cook", 1, parent: "sess_puls_bobotie#1") with { At = now });
            sink.Record(Row(0, "sess_puls_bobotie", 1, purpose: UsagePurpose.SubSession, requests: 1) with { At = now });

            var recording = new BenchmarkRecorder(store, ledgerDirectory: directory).RecordAsync(run);
            string At(object e) => "@" + JsonSerializer.Serialize(e);
            run.Add(At(new { @event = "protocol", version = 1 }));
            run.Add(At(new { @event = "question", mode = "puls-cook", repeat = 1, task = "bobotie", turn = 1, session = "sess_puls_bobotie", text = "?" }));
            run.Add(At(new { @event = "turn", mode = "puls-cook", repeat = 1, task = "bobotie", turn = 1, session = "sess_puls_bobotie", question = "?" }));
            run.Complete(0);
            await recording;

            var requests = await store.RequestsAsync(Run);
            Assert.That(requests.Select(r => r.Turn), Is.All.EqualTo(new TurnKey("puls-cook", 1, "bobotie", 1)));
            Assert.That(requests.Count, Is.EqualTo(2), "the roll-up isn't a request of its own");
            Assert.That((await store.GetRunAsync(Run))!.Total, Is.EqualTo(new Total(0.02m, "USD", 2 * 5200, 2)));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }
}
