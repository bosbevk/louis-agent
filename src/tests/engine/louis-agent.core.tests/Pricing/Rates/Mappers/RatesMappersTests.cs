using LouisAgent.Core.Pricing.Rates;
using LouisAgent.Core.Pricing.Rates.Mappers;

namespace LouisAgent.Core.Tests.Pricing.Rates.Mappers;

// F14 step 1: a mapper turns a provider's reply into our rates.json format, chosen by RATES_FORMAT; only that format is
// read (ExchangeRatesTests).
public class RatesMappersTests
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
}
