using LouisAgent.Core.Pricing.Rates;
using System.Net;
using System.Text;
using System.Text.Json;
using LouisAgent.Core.Config;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Providers;
using LouisAgent.Core.Agent;
using Microsoft.Extensions.AI;
using LouisAgent.Core.Tools.Web;
using LouisAgent.Core.Tools.Web.Search;
using LouisAgent.Core.Tools.Web.Rendering;
using LouisAgent.Core.Tools.Web.Rendering.Providers;
using LouisAgent.Core.Tools.Web.Search.Providers;

using LouisAgent.Core.Tools.Currency;

namespace LouisAgent.Core.Tests.Tools.Currency;

// ConvertCurrency (F22 step 5): the rates the app shows costs in (F14), through the base; nothing without them.
public class CurrencyToolsTests
{
    [Test]
    public void ConvertCurrency_UsesTheLoadedRates_ThroughTheBase()
    {
        // F22 step 5: the rates the app shows costs in (F14), base USD: 1 USD = 18 ZAR, 0.9 EUR.
        var rates = new LouisAgent.Core.Pricing.Rates.ExchangeRates { AsOf = "2026-10-13", Base = "USD", Source = "frankfurter.app", Rates = new() { ["ZAR"] = 18m, ["EUR"] = 0.9m } };
        var tools = new CurrencyTools(rates);

        Assert.That(tools.ConvertCurrency(1800m, "zar", "USD"), Is.EqualTo("1,800.00 ZAR = 100.00 USD (1 ZAR = 0.055556 USD; rates of 2026-10-13, frankfurter.app)."));
        Assert.That(tools.ConvertCurrency(90m, "EUR", "ZAR"), Does.StartWith("90.00 EUR = 1,800.00 ZAR"), "neither is the base");
        Assert.That(tools.ConvertCurrency(1m, "XYZ", "USD"), Does.StartWith("No rate for 'XYZ'").And.Contain("ZAR"));
    }

    [Test]
    public void ConvertCurrency_WithoutRates_SaysSo()
    {
        Assert.That(new CurrencyTools().ConvertCurrency(1m, "ZAR", "USD"), Does.Contain("No exchange rates are loaded"));
    }
}
