using System.Globalization;
using louis_agent.core.config;
using louis_agent.core.usage;

namespace louis_agent.core.tests;

// F14 step 2: the currency costs are shown in, chosen from DISPLAY_CURRENCY, then HOST_CULTURE (the PC's Windows
// culture, passed to containers as a value), then the process's own culture, then the price table's.
public class DisplayCurrencyTests
{
    private static readonly ExchangeRates Rates = ExchangeRates.Parse(
        """{ "as_of": "2026-10-09", "base": "USD", "rates": { "ZAR": 16.5331, "EUR": 0.89238, "GBP": 0.75641 } }""", "test");

    // A container: no culture of its own.
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    private static (DisplayCurrency Display, string Stderr) Resolve(AgentOptions agent, CultureInfo? process = null, ExchangeRates? rates = null)
    {
        var stderr = new StringWriter();
        var original = Console.Error;
        Console.SetError(stderr);
        try
        {
            return (DisplayCurrency.Resolve(agent, "USD", rates ?? Rates, process ?? Invariant), stderr.ToString());
        }
        finally
        {
            Console.SetError(original);
        }
    }

    [Test]
    public void HostCulture_EnZa_IsRandsAtTheRate()
    {
        var (display, _) = Resolve(new AgentOptions { HostCulture = "en-ZA" });

        Assert.That(display, Is.EqualTo(new DisplayCurrency("ZAR", "R", 16.5331m, "2026-10-09", "USD", "HOST_CULTURE en-ZA")));
        Assert.That(display.Converts, Is.True);
        Assert.That(display.ToString(), Is.EqualTo("ZAR (R) from HOST_CULTURE en-ZA at 16.5331 ZAR/USD, as of 2026-10-09"));
    }

    [Test]
    public void DisplayCurrency_WinsOverTheCulture()
    {
        var (display, _) = Resolve(new AgentOptions { DisplayCurrency = "eur", HostCulture = "en-ZA" });

        Assert.That((display.Code, display.Symbol, display.Rate, display.Source), Is.EqualTo(("EUR", "€", 0.89238m, "DISPLAY_CURRENCY")));
    }

    [Test]
    public void ProcessCulture_UsedWhenNothingIsSet()
    {
        // A host run natively on the PC (dotnet run): its own culture is the Windows one.
        var (display, _) = Resolve(new AgentOptions(), CultureInfo.GetCultureInfo("en-GB"));

        Assert.That((display.Code, display.Symbol, display.Source), Is.EqualTo(("GBP", "£", "the culture en-GB")));
    }

    [Test]
    public void InvariantProcessCulture_NothingSet_IsTheTablesCurrency()
    {
        // A container with no HOST_CULTURE: shown as charged, as before F14.
        var (display, stderr) = Resolve(new AgentOptions());

        Assert.That(display, Is.EqualTo(DisplayCurrency.Charged("USD")));
        Assert.That(display.Converts, Is.False);
        Assert.That(stderr, Is.Empty);
    }

    [Test]
    public void HostCulture_InTheTablesCurrency_DoesNotConvert()
    {
        var (display, _) = Resolve(new AgentOptions { HostCulture = "en-US" });

        Assert.That((display.Code, display.Symbol, display.Converts), Is.EqualTo(("USD", "$", false)));
    }

    [TestCase("en", "names no region")]
    [TestCase("not a culture!", "ignoring it")]
    public void HostCulture_NoRegion_WarnsAndFallsThrough(string name, string why)
    {
        var (display, stderr) = Resolve(new AgentOptions { HostCulture = name }, CultureInfo.GetCultureInfo("en-GB"));

        Assert.That(stderr, Does.Contain("[WARN] HOST_CULTURE").And.Contain(why));
        Assert.That(display.Code, Is.EqualTo("GBP"), "falls through to the process culture");
    }

    [Test]
    public void DisplayCurrency_NotACode_WarnsAndFallsThrough()
    {
        var (display, stderr) = Resolve(new AgentOptions { DisplayCurrency = "rand", HostCulture = "en-ZA" });

        Assert.That(stderr, Does.Contain("[WARN] DISPLAY_CURRENCY 'rand'"));
        Assert.That(display.Code, Is.EqualTo("ZAR"));
    }

    [Test]
    public void NoRateForTheCurrency_ShownAsChargedWithAWarning()
    {
        // Never a guessed rate.
        var (display, stderr) = Resolve(new AgentOptions { DisplayCurrency = "CHF" });

        Assert.That(display, Is.EqualTo(DisplayCurrency.Charged("USD")));
        Assert.That(stderr, Does.Contain("[WARN] Display currency: no CHF rate"));
    }

    [Test]
    public void NoRatesAtAll_ShownAsCharged()
    {
        var (display, _) = Resolve(new AgentOptions { HostCulture = "en-ZA" }, rates: ExchangeRates.Empty);

        Assert.That(display.Converts, Is.False);
    }

    [TestCase("USD", "$")]
    [TestCase("ZAR", "R")]
    [TestCase("EUR", "€")]
    [TestCase("GBP", "£")]
    [TestCase("XYZ", "XYZ")]
    public void SymbolFor_FromDotNetsRegionData(string code, string symbol)
    {
        Assert.That(DisplayCurrency.SymbolFor(code), Is.EqualTo(symbol));
    }

    [Test]
    public void Settings_AreRead()
    {
        var options = AgentOptions.FromEnvironment(name => name switch
        {
            "HOST_CULTURE" => "en-ZA",
            "DISPLAY_CURRENCY" => "EUR",
            _ => null,
        });

        Assert.That((options.HostCulture, options.DisplayCurrency), Is.EqualTo(("en-ZA", "EUR")));
    }
}