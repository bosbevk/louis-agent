using LouisAgent.Core.Pricing.Prices;
using LouisAgent.Core.Usage.Records;
using LouisAgent.Core.Usage.Sinks;
using LouisAgent.Core.Usage.Reports;
using System.Text.Json;

namespace LouisAgent.Core.Tests.Usage.Reports;

// Reading a ledger back, pricing records written before F2, and totalling them (the M1 baseline).
public class UsageReportTests
{
    private string _dir = null!;

    [SetUp]
    public void SetUp() => _dir = Directory.CreateTempSubdirectory("usage-report-").FullName;

    [TearDown]
    public void TearDown() => Directory.Delete(_dir, recursive: true);

    private static readonly PriceTable Prices = new()
    {
        AsOf = "2026-10-10",
        Models = new()
        {
            ["claude-haiku-5-5"] = new ModelPrice
            { Input = 0.10m, Output = 0.50m, CacheRead = 0.01m, CacheWrite5m = 0.125m, CacheWrite1h = 0.20m },
        },
    };

    // Three lines from a demo ledger (written before F2, so no "cost"): two fix requests and a triage, plus one unpriced model.
    private const string Ledger = """
                                  {"at":"2026-10-10T17:43:13+00:00","round":1,"purpose":"triage","host":"orchestrator","session":"triage_83d0","turn":1,"task":null,"run":"run-1","service":"order-service","model":"claude-haiku-5-5","tokens":{"input":7000,"cache_write":null,"cache_read":0,"output":300,"reasoning":null},"duration_ms":1,"stop":"tool_calls"}
                                  {"at":"2026-10-10T17:43:20+00:00","round":1,"purpose":"turn","host":"api","session":"sess_1","turn":1,"task":"fix:83d0","run":"run-1","service":"order-service","model":"claude-haiku-5-5","tokens":{"input":47605,"cache_write":null,"cache_read":0,"output":470,"reasoning":null},"duration_ms":1,"stop":"tool_calls"}

                                  {"at":"2026-10-10T17:43:25+00:00","round":2,"purpose":"turn","host":"api","session":"sess_1","turn":1,"task":"fix:83d0","run":"run-1","service":"order-service","model":"claude-haiku-5-5","tokens":{"input":48000,"cache_write":null,"cache_read":0,"output":200,"reasoning":null},"duration_ms":1,"stop":"stop"}
                                  {"at":"2026-10-10T17:44:00+00:00","round":1,"purpose":"turn","host":"cli","session":"cli_1","turn":1,"task":null,"run":null,"service":null,"model":"gpt-5","tokens":{"input":1000,"cache_write":null,"cache_read":null,"output":10,"reasoning":null},"duration_ms":1,"stop":"stop"}
                                  """;

    private string WriteLedger(string text)
    {
        string path = Path.Combine(_dir, "usage-2026-10.jsonl");
        File.WriteAllText(path, text);
        return path;
    }

    [Test]
    public void Read_SkipsBlankLines()
    {
        Assert.That(UsageReport.Read(WriteLedger(Ledger)).Count(), Is.EqualTo(4));
    }

    [Test]
    public void Read_BadLine_ThrowsNamingTheLine()
    {
        string path = WriteLedger(Ledger + "\nnot json\n");

        var ex = Assert.Throws<InvalidDataException>(() => UsageReport.Read(path).ToList());
        Assert.That(ex!.Message, Does.Contain("line 6"));
    }

    [Test]
    public void Totals_PerPieceOfWork_PricesOldRecords()
    {
        var records = UsageReport.Price(UsageReport.Read(WriteLedger(Ledger)), Prices).ToList();

        var totals = UsageReport.Totals(records, UsageReport.WorkOf).ToDictionary(t => t.Key);

        Assert.That(totals.Keys, Is.EqualTo(new[] { "triage:triage_83d0", "fix:83d0", "cli_1" }));
        UsageTotal fix = totals["fix:83d0"];
        Assert.That((fix.Requests, fix.Input, fix.Output), Is.EqualTo((2, 95_605L, 670L)));
        // 47,605 × 0.10 + 470 × 0.50 = 0.004996; 48,000 × 0.10 + 200 × 0.50 = 0.004900.
        Assert.That(fix.Cost, Is.EqualTo(0.009896m));
        Assert.That(totals["triage:triage_83d0"].Cost, Is.EqualTo(0.000850m));
    }

    [Test]
    public void Total_UnpricedRecords_AreCountedButNotCosted()
    {
        var records = UsageReport.Price(UsageReport.Read(WriteLedger(Ledger)), Prices).ToList();

        UsageTotal all = UsageReport.Total("all", records);

        Assert.That(all.Requests, Is.EqualTo(4));
        Assert.That(all.Unpriced, Is.EqualTo(1), "gpt-5 has no price");
        Assert.That(all.Cost, Is.EqualTo(0.010746m), "the priced records only");
        Assert.That(UsageReport.Total("gpt", records.Where(r => r.Model == "gpt-5")).Cost, Is.Null, "unknown, not 0");
    }

    [Test]
    public void Total_ALedgerWrittenBeforeStep4c_EqualsTheTotalOfTheSameRowsWithHashes()
    {
        var before = UsageReport.Price(UsageReport.Read(WriteLedger(Ledger)), Prices).ToList();
        var hashed = before.Select(r => r with { PromptHash = new string('b', 64) }).ToList();

        Assert.That(before.All(r => r.PromptHash is null), Is.True);
        Assert.That(UsageReport.Total("all", before), Is.EqualTo(UsageReport.Total("all", hashed)));
    }

    [Test]
    public void Price_StoredCost_IsKept()
    {
        var charged = new UsageCost("USD", 1.23m, "2026-09-01");
        var record = UsageReport.Read(WriteLedger(Ledger)).First() with { Cost = charged };

        Assert.That(UsageReport.Price([record], Prices).Single().Cost, Is.SameAs(charged));
    }

    /// <summary>A sub-session's roll-up (F5): one record standing for the specialist's 3 requests, with their charged cost.</summary>
    private static UsageRecord RollUp(UsageCost? cost = null, int requests = 3) => new(
        At: DateTimeOffset.Parse("2026-10-14T10:00:00Z"), Round: 0, Purpose: UsagePurpose.SubSession, Host: "api",
        Session: "sess_parent", Turn: 2, Task: null, Run: null, Service: null, Model: "sub-session:puls",
        Tokens: new UsageTokens(1_000, 0, 9_000, 200, null), DurationMs: 4_000, Stop: "sub_session",
        Cost: cost ?? new UsageCost("USD", 0.0123m, null), Requests: requests);

    [Test]
    public void Total_ARollUpRecord_CountsItsRequests()
    {
        // F5 follow-up: a turn that delegated once made 1 request itself and 3 through the specialist, so it says 4.
        UsageRecord own = UsageReport.Read(WriteLedger(Ledger)).Skip(1).First() with
        {
            Cost = new UsageCost("USD", 0.005m, null)
        };

        UsageTotal total = UsageReport.Total("turn", [own, RollUp()]);

        Assert.That(total.Requests, Is.EqualTo(4));
        Assert.That((total.Input, total.CacheRead, total.Output), Is.EqualTo((48_605L, 9_000L, 670L)));
        Assert.That(total.Cost, Is.EqualTo(0.0173m));
        Assert.That(total.Unpriced, Is.Zero);
    }

    [Test]
    public void Total_AnUnpricedRollUp_CountsItsRequestsAsUnpriced()
    {
        // A specialist on an unpriced model: "3 requests unpriced", not 1, so the partial cost isn't read as the whole.
        UsageTotal total = UsageReport.Total("turn", [RollUp(cost: null) with { Cost = null }]);

        Assert.That((total.Requests, total.Unpriced, total.Cost), Is.EqualTo((3, 3, (decimal?)null)));
    }

    [Test]
    public void Json_ANormalRecord_HasNoRequestsField()
    {
        // The ledger format of a model request's row is unchanged: requests is only written on a roll-up.
        UsageRecord record = UsageReport.Read(WriteLedger(Ledger)).First();

        Assert.That(JsonSerializer.Serialize(record, JsonlUsageSink.Json), Does.Not.Contain("\"requests\""));
    }

    [Test]
    public void Json_ARollUp_RoundTripsRequests()
    {
        string line = JsonSerializer.Serialize(RollUp(), JsonlUsageSink.Json);

        UsageRecord back = UsageReport.Read(WriteLedger(line + "\n")).Single();

        Assert.That(line, Does.Contain("\"requests\":3").And.Contain("\"purpose\":\"sub_session\""));
        Assert.That((back.Requests, back.Purpose, back.Cost?.Amount),
            Is.EqualTo(((int?)3, "sub_session", (decimal?)0.0123m)));
    }

    [Test]
    public void Sum_AddsTotals_CostUnknownOnlyWhenNoneHasOne()
    {
        // F3: the orchestrator adds up the usage louis-agent reports per reply, and triage + fix per error.
        var priced = new UsageTotal("a", 3, 100, 10, 1_000, 50, 0.01m, 0);
        var unpriced = new UsageTotal("b", 1, 200, 0, 0, 20, null, 1);

        Assert.That(UsageReport.Sum("sum", [priced, unpriced]),
            Is.EqualTo(new UsageTotal("sum", 4, 300, 10, 1_000, 70, 0.01m, 1)));
        Assert.That(UsageReport.Sum("sum", [unpriced, unpriced]).Cost, Is.Null);
        Assert.That(UsageReport.Sum("sum", []), Is.EqualTo(new UsageTotal("sum", 0, 0, 0, 0, 0, null, 0)));
    }
}
