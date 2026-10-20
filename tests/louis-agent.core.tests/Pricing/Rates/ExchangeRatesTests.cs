using LouisAgent.Core.Pricing.Rates;

namespace LouisAgent.Core.Tests.Pricing.Rates;

// F14 step 1: our rates.json format is the only one read (the mappers that turn a provider's reply into it are
// tested in Mappers/RatesMappersTests).
public class ExchangeRatesTests
{
    // A real reply from https://api.frankfurter.dev/v1/latest?base=USD (2026-10-11), trimmed to four currencies.
    private const string FrankfurterReply =
        """{"amount":1.0,"base":"USD","date":"2026-10-09","rates":{"EUR":0.89238,"GBP":0.75641,"JPY":158.25,"ZAR":16.5331}}""";

    private const string OurFormat = """
        // comments are allowed, as in prices.json
        { "as_of": "2026-10-09", "base": "USD", "source": "test", "rates": { "ZAR": 16.5331, "EUR": 0.89238, } }
        """;

    [Test]
    public void Parse_ReadsOurFormat_WithCommentsAndTrailingCommas()
    {
        ExchangeRates rates = ExchangeRates.Parse(OurFormat, "test");

        Assert.That((rates.AsOf, rates.Base, rates.Source, rates.Rates.Count), Is.EqualTo(("2026-10-09", "USD", "test", 2)));
    }

    [Test]
    public void Parse_AProvidersReply_IsNotReadAsOurs()
    {
        // The whole point of the mappers: only our format is read, so Frankfurter's shape is rejected, naming the source.
        var ex = Assert.Throws<InvalidDataException>(() => ExchangeRates.Parse(FrankfurterReply, "https://x/rates"));

        Assert.That(ex!.Message, Does.Contain("not in rates.json format").And.Contain("https://x/rates"));
    }

    [TestCase("""{ "as_of": "2026-10-09", "base": "USD", "rates": { "ZAR": 0 } }""", "must be positive")]
    [TestCase("""{ "as_of": "2026-10-09", "base": "USD", "rates": { "ZAR": -1 } }""", "must be positive")]
    [TestCase("""{ "as_of": "2026-10-09", "base": "USD", "rates": { "rand": 16.5 } }""", "not a currency code")]
    [TestCase("""{ "as_of": "2026-10-09", "base": "usd", "rates": { "ZAR": 16.5 } }""", "not a currency code")]
    [TestCase("""{ "base": "USD", "rates": { "ZAR": 16.5 } }""", "as_of")]
    public void Parse_InvalidRates_ThrowSaying(string json, string reason)
    {
        var ex = Assert.Throws<InvalidDataException>(() => ExchangeRates.Parse(json, "rates.json"));

        Assert.That(ex!.Message, Does.Contain(reason));
    }

    [Test]
    public void RateFor_BaseIsOne_UnknownIsNull_CaseIgnored()
    {
        ExchangeRates rates = ExchangeRates.Parse(OurFormat, "test");

        Assert.That(rates.RateFor("USD"), Is.EqualTo(1m));
        Assert.That(rates.RateFor("zar"), Is.EqualTo(16.5331m));
        Assert.That(rates.RateFor("CHF"), Is.Null);
    }

    [Test]
    public void ToJson_RoundTrips()
    {
        ExchangeRates rates = ExchangeRates.Parse(OurFormat, "test");

        ExchangeRates again = ExchangeRates.Parse(rates.ToJson(), "test");

        Assert.That((again.AsOf, again.Base, again.Source, again.RateFor("ZAR")), Is.EqualTo((rates.AsOf, rates.Base, rates.Source, rates.RateFor("ZAR"))));
    }

    [Test]
    public void ShippedRatesFile_IsValid_FromUsd_WithRand()
    {
        // config/rates.json is the fallback every clone and container uses offline.
        ExchangeRates rates = ExchangeRates.Load(Path.Combine(TestPaths.RepoRoot, "config", "rates.json"));

        Assert.That(rates.Base, Is.EqualTo("USD"));
        Assert.That(rates.RateFor("ZAR"), Is.GreaterThan(0));
        Assert.That(rates.AsOf, Does.Match(@"^\d{4}-\d{2}-\d{2}$"));
    }
}