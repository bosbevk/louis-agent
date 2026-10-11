using louis_agent.core.usage;

namespace LouisAgent.Core.Tests;

// F14 step 1: our rates.json format is the only one read; a mapper turns a provider's reply into it.
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
    public void FrankfurterMapper_RealReply_BecomesOurFormat()
    {
        string json = new FrankfurterRatesMapper().ToRatesJson(FrankfurterReply, "test");
        ExchangeRates rates = ExchangeRates.Parse(json, "test");

        Assert.That((rates.AsOf, rates.Base, rates.Source), Is.EqualTo(("2026-10-09", "USD", "frankfurter (ECB reference rates)")));
        Assert.That(rates.Rates, Has.Count.EqualTo(4));
        Assert.That(rates.RateFor("ZAR"), Is.EqualTo(16.5331m));
    }

    [Test]
    public void FrankfurterMapper_AmountOtherThanOne_GivesRatesPerOneUnit()
    {
        // ?amount=100 makes Frankfurter answer for 100 USD; ours are always for 1.
        const string reply = """{"amount":100.0,"base":"USD","date":"2026-10-09","rates":{"ZAR":1653.31}}""";

        ExchangeRates rates = ExchangeRates.Parse(new FrankfurterRatesMapper().ToRatesJson(reply, "test"), "test");

        Assert.That(rates.RateFor("ZAR"), Is.EqualTo(16.5331m));
    }

    [Test]
    public void FrankfurterMapper_NotItsFormat_ThrowsNamingTheSource()
    {
        var ex = Assert.Throws<InvalidDataException>(() => new FrankfurterRatesMapper().ToRatesJson("""{"error":"not found"}""", "https://x/rates"));

        Assert.That(ex!.Message, Does.Contain("https://x/rates"));
    }

    [Test]
    public void RatesJsonMapper_PassesOurFormatThrough()
    {
        Assert.That(new RatesJsonMapper().ToRatesJson(OurFormat, "test"), Is.SameAs(OurFormat));
    }

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

    [TestCase(null, typeof(FrankfurterRatesMapper))]
    [TestCase("frankfurter", typeof(FrankfurterRatesMapper))]
    [TestCase("Rates-Json", typeof(RatesJsonMapper))]
    public void RatesMappers_ChooseByFormat(string? format, Type expected)
    {
        Assert.That(RatesMappers.For(format), Is.TypeOf(expected));
    }

    [Test]
    public void RatesMappers_UnknownFormat_Throws()
    {
        Assert.That(() => RatesMappers.For("xe"), Throws.TypeOf<InvalidDataException>().With.Message.Contain("RATES_FORMAT"));
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