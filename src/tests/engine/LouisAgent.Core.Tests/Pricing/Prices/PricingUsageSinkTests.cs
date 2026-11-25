using LouisAgent.Core.Llm.UsageMapping;
using LouisAgent.Core.Pricing.Prices;
using LouisAgent.Core.Usage.Records;
using LouisAgent.Core.Usage.Sinks;
using System.Text.Json;
using LouisAgent.Core.Config;
using LouisAgent.Core.Agent;
using Microsoft.Extensions.AI;
using LouisAgent.Core.Llm;
using LouisAgent.Core.Anthropic;
using LouisAgent.Core.Hosting;

namespace LouisAgent.Core.Tests.Pricing.Prices;

/// <summary>
/// Every ledger record carries its cost, priced from one file of prices (PRICES_FILE, or config/prices.json).
/// </summary>
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

    /// <summary>Each test gets a folder of its own for the ledger and the prices.</summary>
    [SetUp]
    public void SetUp()
    {
        _dir = Directory.CreateTempSubdirectory("pricing-sink-").FullName;
        _prices = Path.Combine(_dir, "prices.json");
        File.WriteAllText(_prices, Prices);
    }

    /// <summary>The folder goes when the test is done.</summary>
    [TearDown]
    public void TearDown() => Directory.Delete(_dir, recursive: true);

    /// <summary>A request's record on the model named, priced or not.</summary>
    private static UsageRecord Record(string? model, UsageCost? cost = null) => new(
        At: DateTimeOffset.UtcNow, Round: 1, Purpose: UsagePurpose.Turn, Host: "api", Session: "sess_1", Turn: 1,
        Task: null,
        Run: null, Service: null, Model: model, Tokens: new UsageTokens(47_605, null, 0, 470, null), DurationMs: 1,
        Stop: "stop",
        Cost: cost);

    /// <summary>A record is priced before it reaches the ledger behind.</summary>
    [Test]
    public void Record_PricesTheRecordBeforeTheInnerSink()
    {
        var inner = new ListUsageSink();

        new PricingUsageSink(inner, PriceTable.Load(_prices)).Record(Record("claude-haiku-5-5"));

        Assert.That(inner.Records.Single().Cost, Is.EqualTo(new UsageCost("USD", 0.004996m, "2026-10-10")));
    }

    /// <summary>A model with no price goes through with its cost unknown.</summary>
    [Test]
    public void Record_UnknownModel_GoesThroughUnpriced()
    {
        var inner = new ListUsageSink();

        new PricingUsageSink(inner, PriceTable.Load(_prices)).Record(Record("gpt-5"));

        Assert.That(inner.Records.Single().Cost, Is.Null);
    }

    /// <summary>A record that already has a cost keeps it.</summary>
    [Test]
    public void Record_AlreadyPriced_KeepsItsCost()
    {
        // A cost is stored with the prices it was charged at; a newer table never rewrites it.
        var inner = new ListUsageSink();
        var charged = new UsageCost("USD", 0.123m, "2026-09-01");

        new PricingUsageSink(inner, PriceTable.Load(_prices)).Record(Record("claude-haiku-5-5", charged));

        Assert.That(inner.Records.Single().Cost, Is.SameAs(charged));
    }

    /// <summary>Whoever listens for a new record sees it with its cost.</summary>
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

    /// <summary>A host built the usual way writes ledger lines that carry their cost.</summary>
    [Test]
    public async Task AgentHostBuild_LedgerLinesCarryTheirCost()
    {
        // End to end: AgentHost.Build → the engine's recorder → PricingUsageSink → the JSONL file.
        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, "hi"))
        {
            ModelId = "claude-haiku-5-5",
            Usage = new UsageDetails
            {
                InputTokenCount = 47_605,
                CachedInputTokenCount = 0,
                OutputTokenCount = 470
            },
        };
        AgentOptions agent = TestPaths.Agent();
        agent.LogDirectory = _dir;
        agent.PricesFile = _prices;

        AgentEngine engine = AgentHost
            .Build(new LlmOptions(), agent, new StubClientFactory(new FakeChatClient(_ => response))).Engine;
        await engine.ProcessPromptAsync(engine.StartConversation(), "hi");

        string line = File.ReadAllLines(Path.Combine(_dir, JsonlUsageSink.FileNameFor(DateTimeOffset.UtcNow))).Single();
        using var json = JsonDocument.Parse(line);
        JsonElement cost = json.RootElement.GetProperty("cost");
        Assert.That(cost.GetProperty("amount").GetDecimal(), Is.EqualTo(0.004996m));
        Assert.That(cost.GetProperty("price_table").GetString(), Is.EqualTo("2026-10-10"));
    }

    /// <summary>Without a prices file the host warns, and records with no cost.</summary>
    [Test]
    public void LoadPrices_NoFile_WarnsAndRecordsWithoutCost()
    {
        var stderr = new StringWriter();
        var original = Console.Error;
        Console.SetError(stderr);
        PriceTable prices;
        try
        {
            prices = PriceTableLoader.Load(new AgentOptions { PricesFile = Path.Combine(_dir, "missing.json") });
        }
        finally
        {
            Console.SetError(original);
        }

        Assert.That(prices.IsEmpty);
        Assert.That(stderr.ToString(), Does.Contain("[WARN] No price table").And.Contain("missing.json"));
    }

    /// <summary>A prices file that can't be read stops the host, rather than record wrong costs.</summary>
    [Test]
    public void CreateUsageSink_MalformedPrices_StopsTheHost()
    {
        File.WriteAllText(_prices, "{ not json");

        Assert.Throws<InvalidDataException>(() => UsageLedger.Open(new AgentOptions
        {
            LogDirectory = _dir,
            PricesFile = _prices
        }));
    }

    /// <summary>PRICES_FILE is read from the environment.</summary>
    [Test]
    public void PricesFileSetting_IsReadFromTheEnvironment()
    {
        var options = AgentOptions.FromEnvironment(name => name == "PRICES_FILE" ? "/config/prices.json" : null);

        Assert.That(options.PricesFile, Is.EqualTo("/config/prices.json"));
    }

    /// <summary>A client factory that hands back the fake model and Anthropic's usage mapper.</summary>
    private sealed class StubClientFactory(IChatClient client) : ILlmClientFactory
    {
        /// <summary>The fake model, whatever is asked for.</summary>
        public IChatClient Create(LlmOptions options) => client;

        /// <summary>Anthropic's mapper, so the records look like a real provider's.</summary>
        public IUsageMapper CreateUsageMapper(LlmOptions options) => new AnthropicUsageMapper();
    }
}
