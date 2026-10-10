using louis_agent.core.usage;

namespace louis_agent.core.tests;

// F2-S1: prices in one config file, matched to the model by longest prefix.
public class PriceTableTests
{
    private string _dir = null!;

    [SetUp]
    public void SetUp() => _dir = Directory.CreateTempSubdirectory("prices-").FullName;

    [TearDown]
    public void TearDown() => Directory.Delete(_dir, recursive: true);

    private string Write(string json)
    {
        string path = Path.Combine(_dir, "prices.json");
        File.WriteAllText(path, json);
        return path;
    }

    private const string Table = """
        {
          // Copied from the provider's pricing page; comments are allowed.
          "as_of": "2026-10-10",
          "currency": "USD",
          "models": {
            "claude-haiku": { "input": 9, "output": 9, "cache_read": 9, "cache_write_5m": 9, "cache_write_1h": 9 },
            "claude-haiku-4-5": { "input": 1.00, "output": 5.00, "cache_read": 0.10, "cache_write_5m": 1.25, "cache_write_1h": 2.00 },
            "claude-haiku-5-5": {
              "input": 0.10, "output": 0.50, "cache_read": 0.01, "cache_write_5m": 0.125, "cache_write_1h": 0.20,
              "long_prompt": { "above_tokens": 100000, "input": 0.50, "output": 2.50, "cache_read": 0.05, "cache_write_5m": 0.625, "cache_write_1h": 1.00 },
            },
            "llama": { "input": 0, "output": 0, "cache_read": 0, "cache_write_5m": 0, "cache_write_1h": 0 }
          }
        }
        """;

    [Test]
    public void Load_ReadsDateCurrencyAndPrices()
    {
        PriceTable table = PriceTable.Load(Write(Table));

        Assert.That(table.AsOf, Is.EqualTo("2026-10-10"));
        Assert.That(table.Currency, Is.EqualTo("USD"));
        Assert.That(table.Find("claude-haiku-4-5")!.Output, Is.EqualTo(5.00m));
        Assert.That(table.Find("claude-haiku-4-5")!.CacheWrite1h, Is.EqualTo(2.00m));
    }

    [Test]
    public void Find_DatedModelId_MatchesItsPrefix()
    {
        PriceTable table = PriceTable.Load(Write(Table));

        Assert.That(table.Find("claude-haiku-4-5-20251001")!.Input, Is.EqualTo(1.00m));
    }

    [Test]
    public void Find_LongestPrefixWins()
    {
        // "claude-haiku" also matches, but "claude-haiku-5-5" is the more specific entry.
        PriceTable table = PriceTable.Load(Write(Table));

        Assert.That(table.Find("claude-haiku-5-5")!.Input, Is.EqualTo(0.10m));
        Assert.That(table.Find("claude-haiku-3")!.Input, Is.EqualTo(9m), "only the short prefix matches");
    }

    [Test]
    public void Find_UnknownOrMissingModel_IsNull()
    {
        PriceTable table = PriceTable.Load(Write(Table));

        Assert.That(table.Find("gpt-5"), Is.Null);
        Assert.That(table.Find(null), Is.Null);
    }

    [Test]
    public void Load_LongPromptTier_IsParsed()
    {
        ModelPrice haiku55 = PriceTable.Load(Write(Table)).Find("claude-haiku-5-5")!;

        Assert.That(haiku55.LongPrompt!.AboveTokens, Is.EqualTo(100_000));
        Assert.That(haiku55.LongPrompt.Output, Is.EqualTo(2.50m));
        Assert.That(PriceTable.Load(Write(Table)).Find("claude-haiku-4-5")!.LongPrompt, Is.Null);
    }

    [Test]
    public void Load_LocalModelPricedAtZero_IsAPriceNotAGap()
    {
        ModelPrice? llama = PriceTable.Load(Write(Table)).Find("llama3.1");

        Assert.That(llama, Is.Not.Null);
        Assert.That(llama!.Input, Is.Zero);
    }

    [Test]
    public void Load_MissingFile_IsAnEmptyTable()
    {
        PriceTable table = PriceTable.Load(Path.Combine(_dir, "nope.json"));

        Assert.That(table.IsEmpty);
        Assert.That(table.Find("claude-haiku-5-5"), Is.Null);
    }

    [Test]
    public void Load_MalformedJson_ThrowsNamingTheFile()
    {
        string path = Write("{ \"models\": ");

        var ex = Assert.Throws<InvalidDataException>(() => PriceTable.Load(path));
        Assert.That(ex!.Message, Does.Contain(path));
    }

    [Test]
    public void Load_MissingPrice_Throws()
    {
        // A missing output price would otherwise read as 0, i.e. free: refuse it.
        string path = Write("""{ "models": { "m": { "input": 1, "cache_read": 0, "cache_write_5m": 0, "cache_write_1h": 0 } } }""");

        var ex = Assert.Throws<InvalidDataException>(() => PriceTable.Load(path));
        Assert.That(ex!.Message, Does.Contain("output"));
    }

    [Test]
    public void Load_NegativePrice_ThrowsNamingTheModelAndPrice()
    {
        string path = Write("""{ "models": { "m": { "input": -1, "output": 1, "cache_read": 0, "cache_write_5m": 0, "cache_write_1h": 0 } } }""");

        var ex = Assert.Throws<InvalidDataException>(() => PriceTable.Load(path));
        Assert.That(ex!.Message, Does.Contain("'m'").And.Contain("input"));
    }

    [Test]
    public void Load_NoCurrency_DefaultsToUsd()
    {
        string path = Write("""{ "models": {} }""");

        Assert.That(PriceTable.Load(path).Currency, Is.EqualTo("USD"));
    }
}