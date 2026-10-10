using louis_agent.core.usage;

namespace louis_agent.core.tests;

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
            ["claude-haiku-5-5"] = new ModelPrice { Input = 0.10m, Output = 0.50m, CacheRead = 0.01m, CacheWrite5m = 0.125m, CacheWrite1h = 0.20m },
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
    public void Price_StoredCost_IsKept()
    {
        var charged = new UsageCost("USD", 1.23m, "2026-09-01");
        var record = UsageReport.Read(WriteLedger(Ledger)).First() with { Cost = charged };

        Assert.That(UsageReport.Price([record], Prices).Single().Cost, Is.SameAs(charged));
    }
}