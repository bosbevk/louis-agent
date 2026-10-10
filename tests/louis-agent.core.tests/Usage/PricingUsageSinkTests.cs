using System.Text.Json;
using louis_agent.core.config;
using louis_agent.core.providers;
using louis_agent.core.tools;
using louis_agent.core.usage;
using Microsoft.Extensions.AI;

namespace louis_agent.core.tests;

// F2-S2: every ledger record carries its cost. F2-S1: prices come from one file (PRICES_FILE or config/prices.json).
public class PricingUsageSinkTests
{
    private const string Prices = """
        {
          "as_of": "2026-10-10",
          "currency": "USD",
          "models": {
            "claude-haiku-5-5": { "input": 0.10, "output": 0.50, "cache_read": 0.01, "cache_write_5m": 0.125, "cache_write_1h": 0.20 }
          }
        }
        """;

    private string _dir = null!;
    private string _prices = null!;

    [SetUp]
    public void SetUp()
    {
        _dir = Directory.CreateTempSubdirectory("pricing-sink-").FullName;
        _prices = Path.Combine(_dir, "prices.json");
        File.WriteAllText(_prices, Prices);
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_dir, recursive: true);

    private static UsageRecord Record(string? model, UsageCost? cost = null) => new(
        At: DateTimeOffset.UtcNow, Round: 1, Purpose: UsagePurpose.Turn, Host: "api", Session: "sess_1", Turn: 1, Task: null,
        Run: null, Service: null, Model: model, Tokens: new UsageTokens(47_605, null, 0, 470, null), DurationMs: 1, Stop: "stop",
        Cost: cost);

    [Test]
    public void Record_PricesTheRecordBeforeTheInnerSink()
    {
        var inner = new ListUsageSink();

        new PricingUsageSink(inner, PriceTable.Load(_prices)).Record(Record("claude-haiku-5-5"));

        Assert.That(inner.Records.Single().Cost, Is.EqualTo(new UsageCost("USD", 0.004996m, "2026-10-10")));
    }

    [Test]
    public void Record_UnknownModel_GoesThroughUnpriced()
    {
        var inner = new ListUsageSink();

        new PricingUsageSink(inner, PriceTable.Load(_prices)).Record(Record("gpt-5"));

        Assert.That(inner.Records.Single().Cost, Is.Null);
    }

    [Test]
    public void Record_AlreadyPriced_KeepsItsCost()
    {
        // A cost is stored with the prices it was charged at; a newer table never rewrites it.
        var inner = new ListUsageSink();
        var charged = new UsageCost("USD", 0.123m, "2026-09-01");

        new PricingUsageSink(inner, PriceTable.Load(_prices)).Record(Record("claude-haiku-5-5", charged));

        Assert.That(inner.Records.Single().Cost, Is.SameAs(charged));
    }

    [Test]
    public void Record_LedgersUsageRecordedEvent_SeesTheCost()
    {
        // F3 will show live totals from this event, so it must get the priced record.
        var ledger = new JsonlUsageSink(_dir);
        var seen = new List<UsageRecord>();
        ledger.UsageRecorded += seen.Add;

        new PricingUsageSink(ledger, PriceTable.Load(_prices)).Record(Record("claude-haiku-5-5"));

        Assert.That(seen.Single().Cost!.Amount, Is.EqualTo(0.004996m));
    }

    [Test]
    public async Task AgentHostBuild_LedgerLinesCarryTheirCost()
    {
        // End to end: AgentHost.Build → the engine's recorder → PricingUsageSink → the JSONL file.
        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, "hi"))
        {
            ModelId = "claude-haiku-5-5",
            Usage = new UsageDetails { InputTokenCount = 47_605, CachedInputTokenCount = 0, OutputTokenCount = 470 },
        };
        AgentOptions agent = TestPaths.Agent();
        agent.LogDirectory = _dir;
        agent.PricesFile = _prices;

        AgentEngine engine = AgentHost.Build(new LlmOptions(), agent, new StubClientFactory(new FakeChatClient(_ => response))).Engine;
        await engine.ProcessPromptAsync(engine.NewHistory(), "hi");

        string line = File.ReadAllLines(Path.Combine(_dir, JsonlUsageSink.FileNameFor(DateTimeOffset.UtcNow))).Single();
        using var json = JsonDocument.Parse(line);
        JsonElement cost = json.RootElement.GetProperty("cost");
        Assert.That(cost.GetProperty("amount").GetDecimal(), Is.EqualTo(0.004996m));
        Assert.That(cost.GetProperty("price_table").GetString(), Is.EqualTo("2026-10-10"));
    }

    [Test]
    public void LoadPrices_NoFile_WarnsAndRecordsWithoutCost()
    {
        var stderr = new StringWriter();
        var original = Console.Error;
        Console.SetError(stderr);
        PriceTable prices;
        try
        {
            prices = AgentHost.LoadPrices(new AgentOptions { PricesFile = Path.Combine(_dir, "missing.json") });
        }
        finally
        {
            Console.SetError(original);
        }

        Assert.That(prices.IsEmpty);
        Assert.That(stderr.ToString(), Does.Contain("[WARN] No price table").And.Contain("missing.json"));
    }

    [Test]
    public void CreateUsageSink_MalformedPrices_StopsTheHost()
    {
        File.WriteAllText(_prices, "{ not json");

        Assert.Throws<InvalidDataException>(() => AgentHost.CreateUsageSink(new AgentOptions { LogDirectory = _dir, PricesFile = _prices }));
    }

    [Test]
    public void PricesFileSetting_IsReadFromTheEnvironment()
    {
        var options = AgentOptions.FromEnvironment(name => name == "PRICES_FILE" ? "/config/prices.json" : null);

        Assert.That(options.PricesFile, Is.EqualTo("/config/prices.json"));
    }

    private sealed class StubClientFactory(IChatClient client) : ILlmClientFactory
    {
        public IChatClient Create(LlmOptions options) => client;
        public IUsageMapper CreateUsageMapper(LlmOptions options) => new AnthropicUsageMapper();
    }
}