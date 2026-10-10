using louis_agent.core.usage;

namespace louis_agent.core.tests;

// F2-S2: cost on every record from the price table. F2-S3: unknown is not free.
public class CostCalculatorTests
{
    private static readonly TokenPricesFor Haiku45 = new(1.00m, 5.00m, 0.10m, 1.25m, 2.00m);
    private static readonly TokenPricesFor Haiku55 = new(0.10m, 0.50m, 0.01m, 0.125m, 0.20m);
    private static readonly TokenPricesFor Haiku55Long = new(0.50m, 2.50m, 0.05m, 0.625m, 1.00m);

    private record TokenPricesFor(decimal Input, decimal Output, decimal CacheRead, decimal Write5m, decimal Write1h);

    private static ModelPrice Price(TokenPricesFor p, LongPromptPrices? longPrompt = null) => new()
    {
        Input = p.Input, Output = p.Output, CacheRead = p.CacheRead, CacheWrite5m = p.Write5m, CacheWrite1h = p.Write1h,
        LongPrompt = longPrompt,
    };

    private static readonly PriceTable Table = new()
    {
        AsOf = "2026-10-10",
        Currency = "USD",
        Models = new()
        {
            ["claude-haiku-4-5"] = Price(Haiku45),
            ["claude-haiku-5-5"] = Price(Haiku55, new LongPromptPrices
            {
                AboveTokens = 100_000, Input = Haiku55Long.Input, Output = Haiku55Long.Output, CacheRead = Haiku55Long.CacheRead,
                CacheWrite5m = Haiku55Long.Write5m, CacheWrite1h = Haiku55Long.Write1h,
            }),
            ["llama"] = Price(new TokenPricesFor(0, 0, 0, 0, 0)),
        },
    };

    private static UsageRecord Record(string? model, UsageTokens tokens) => new(
        At: DateTimeOffset.UtcNow, Round: 1, Purpose: UsagePurpose.Turn, Host: null, Session: null, Turn: null, Task: null,
        Run: null, Service: null, Model: model, Tokens: tokens, DurationMs: 1, Stop: "stop");

    [Test]
    public void For_SpecsWorkedExample()
    {
        // Usage spec §4.1: 1,840 input + 26,110 cache-write + 412 output on Haiku 4.5 prices ≈ $0.0365.
        UsageCost? cost = CostCalculator.For(Record("claude-haiku-4-5-20251001", new UsageTokens(1_840, 26_110, 0, 412, 210)), Table);

        Assert.That(cost, Is.EqualTo(new UsageCost("USD", 0.036538m, "2026-10-10")));
    }

    [Test]
    public void For_CacheReads_AtTheReadPrice()
    {
        // 39 uncached + 10,227 read + 206 output: the F1 spike's cache-read request.
        UsageCost? cost = CostCalculator.For(Record("claude-haiku-4-5", new UsageTokens(39, null, 10_227, 206, null)), Table);

        // 39 × 1.00 + 10,227 × 0.10 + 206 × 5.00 = 2,091.7 per million.
        Assert.That(cost!.Amount, Is.EqualTo(0.002092m));
    }

    [Test]
    public void For_RealRecordFromTheDemoLedger()
    {
        // A fix request from samples/sample-output: 47,605 input, 0 cached, 470 output on Haiku 5.5.
        UsageCost? cost = CostCalculator.For(Record("claude-haiku-5-5", new UsageTokens(47_605, null, 0, 470, null)), Table);

        Assert.That(cost!.Amount, Is.EqualTo(0.004996m));
    }

    [TestCase(99_999, 0.10)]
    [TestCase(100_000, 0.10)]
    [TestCase(100_001, 0.50)]
    public void For_LongPromptTier_AppliesOnlyAboveTheThreshold(long input, decimal inputPrice)
    {
        UsageCost? cost = CostCalculator.For(Record("claude-haiku-5-5", new UsageTokens(input, null, null, 0, null)), Table);

        Assert.That(cost!.Amount, Is.EqualTo(Math.Round(input * inputPrice / 1_000_000m, 6, MidpointRounding.AwayFromZero)));
    }

    [Test]
    public void For_LongPromptTier_CountsCacheAndPricesTheWholeRequest()
    {
        // 1,000 uncached + 99,500 read = 100,500 prompt tokens: over the threshold, so every kind uses the long prices.
        UsageCost? cost = CostCalculator.For(Record("claude-haiku-5-5", new UsageTokens(1_000, null, 99_500, 2_000, null)), Table);

        // 1,000 × 0.50 + 99,500 × 0.05 + 2,000 × 2.50 = 10,475 per million.
        Assert.That(cost!.Amount, Is.EqualTo(0.010475m));
    }

    [Test]
    public void For_UnknownModel_IsNullNotFree()
    {
        Assert.That(CostCalculator.For(Record("gpt-5", new UsageTokens(1_000, null, null, 100, null)), Table), Is.Null);
        Assert.That(CostCalculator.For(Record(null, new UsageTokens(1_000, null, null, 100, null)), Table), Is.Null);
    }

    [Test]
    public void For_ModelPricedAtZero_IsZeroNotNull()
    {
        UsageCost? cost = CostCalculator.For(Record("llama3.1", new UsageTokens(2_050, null, null, 2, null)), Table);

        Assert.That(cost, Is.EqualTo(new UsageCost("USD", 0m, "2026-10-10")));
    }

    [Test]
    public void For_InputOrOutputNotReported_IsNull()
    {
        Assert.That(CostCalculator.For(Record("claude-haiku-5-5", new UsageTokens(null, null, null, 100, null)), Table), Is.Null);
        Assert.That(CostCalculator.For(Record("claude-haiku-5-5", new UsageTokens(1_000, null, null, null, null)), Table), Is.Null);
    }

    [Test]
    public void For_EmptyTable_IsNull()
    {
        Assert.That(CostCalculator.For(Record("claude-haiku-5-5", new UsageTokens(1_000, null, null, 100, null)), PriceTable.Empty),
            Is.Null);
    }

    [Test]
    public void For_RoundsToSixDecimals()
    {
        // 1 input token on Haiku 5.5 = 0.0000001: rounds to 0.000000, stored as an exact decimal.
        UsageCost? cost = CostCalculator.For(Record("claude-haiku-5-5", new UsageTokens(1, null, null, 0, null)), Table);

        Assert.That(cost!.Amount, Is.EqualTo(0m));
    }
}