using System.Globalization;
using LouisAgent.Core.Usage;
using LouisAgent.Core.Pricing;

namespace LouisAgent.Core.Tests;

// F14 step 3: the usage text in the display currency, always with '.' decimals; the ledger keeps what was charged.
public class UsageTextCurrencyTests
{
    private static readonly DisplayCurrency Rands = new("ZAR", "R", 16.5331m, "2026-10-09", "USD", "HOST_CULTURE en-ZA");
    private static readonly DisplayCurrency Euros = new("EUR", "€", 0.89238m, "2026-10-09", "USD", "DISPLAY_CURRENCY");
    private static readonly DisplayCurrency NoSymbol = new("XYZ", "XYZ", 2m, "2026-10-09", "USD", "DISPLAY_CURRENCY");

    private static UsageRecord Record(decimal? cost, string purpose = UsagePurpose.Turn) => new(
        At: DateTimeOffset.UtcNow, Round: 1, Purpose: purpose, Host: "cli", Session: "s", Turn: 1, Task: null, Run: null,
        Service: null, Model: "claude-haiku-5-5", Tokens: new UsageTokens(1_000, 0, 0, 100, null), DurationMs: 1,
        Stop: "stop", Cost: cost is null ? null : new UsageCost("USD", cost.Value, "2026-10-10"));

    [TestCase("0.006", "R0.099")]
    [TestCase("0.0066", "R0.11")]
    [TestCase("0.044", "R0.73")]
    [TestCase("2.117", "R35.00")]
    [TestCase("0", "R0")]
    public void Money_InRands(string usd, string expected)
    {
        Assert.That(UsageText.Money(decimal.Parse(usd, CultureInfo.InvariantCulture), "USD", Rands), Is.EqualTo(expected));
    }

    [Test]
    public void Money_OtherSymbols()
    {
        Assert.That(UsageText.Money(0.04m, "USD", Euros), Is.EqualTo("€0.036"));
        Assert.That(UsageText.Money(0.04m, "USD", NoSymbol), Is.EqualTo("0.08 XYZ"), "a currency without a symbol follows the number");
    }

    [Test]
    public void Money_NoDisplay_IsAsCharged()
    {
        Assert.That(UsageText.Money(0.006m, "USD"), Is.EqualTo("$0.006"));
        Assert.That(UsageText.Money(0.006m, "USD", DisplayCurrency.Charged("USD")), Is.EqualTo("$0.006"));
    }

    [Test]
    public void Money_EnZaThreadCulture_StillWritesADot()
    {
        // en-ZA writes 35,00; the usage text never does, whatever culture the host thread has.
        CultureInfo original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-ZA");
        try
        {
            Assert.That(UsageText.Money(2.117m, "USD", Rands), Is.EqualTo("R35.00"));
            Assert.That(UsageText.Money(0.0066m, "USD", Rands), Is.EqualTo("R0.11"));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Test]
    public void Line_TurnAndSession_InRands()
    {
        UsageRecord[] turn = [Record(0.0066m)];
        UsageRecord[] session = [Record(0.0374m), .. turn];

        Assert.That(UsageText.Line(turn, session, Rands), Is.EqualTo("turn: 1 request · 1.1k tokens · R0.11 · session R0.73"));
    }

    [Test]
    public void Line_UnknownPriceAndPartialTotals_KeepTheirWords()
    {
        Assert.That(UsageText.Line([Record(null)], display: Rands), Is.EqualTo("turn: 1 request · 1.1k tokens · price unknown"));
        Assert.That(UsageText.Line([Record(0.0066m), Record(null)], display: Rands),
            Is.EqualTo("turn: 2 requests · 2.2k tokens · R0.11 + 1 unpriced"));
    }

    [Test]
    public void Summary_InRands_NamesTheRateAndWhatWasCharged()
    {
        UsageRecord[] session = [Record(0.0066m), Record(0.0374m)];

        Assert.That(UsageText.Summary(session, Rands), Is.EqualTo(
            "Session usage: 2 requests · 2.2k tokens · R0.73\n" +
            "- turn: 2 requests · 2.2k tokens · R0.73\n" +
            "- tokens: 2k input · 0 cache write · 0 cache read · 200 output\n" +
            "- rate: 16.5331 ZAR/USD on 2026-10-09 · $0.044 charged"));
    }

    [Test]
    public void Summary_NotConverted_HasNoRateLine()
    {
        Assert.That(UsageText.Summary([Record(0.0066m)]), Does.Not.Contain("rate:"));
        Assert.That(UsageText.Summary([Record(0.0066m)], DisplayCurrency.Charged("USD")), Does.Not.Contain("rate:"));
    }

    [Test]
    public void Summary_NothingPriced_HasNoRateLine()
    {
        // No cost to convert: a rate line would explain a figure that isn't there.
        Assert.That(UsageText.Summary([Record(null)], Rands), Does.Not.Contain("rate:"));
    }
}
