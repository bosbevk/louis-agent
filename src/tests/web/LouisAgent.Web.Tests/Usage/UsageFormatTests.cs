using LouisAgent.Web.Usage;

namespace LouisAgent.Web.Tests.Usage;

/// <summary>
/// The usage line under an answer and the chat's total, in the display currency. The same number rules as core's
/// UsageText: k/M tokens, two significant digits below 1, "price unknown" rather than 0 when nothing was priced
/// (ADR 0007), always "." as the decimal mark.
/// </summary>
public class UsageFormatTests
{
    private static readonly TurnUsage Priced = new(1_900, 0, 26_000, 412, 3, 0.004m, "USD", 0);
    private static readonly TurnUsage Unpriced = new(1_900, 0, 26_000, 412, 3, null, null, 3);
    private static readonly CurrencyChoice Rand = new("ZAR", "R", 18m, "2026-10-13", "USD");
    private static readonly CurrencyChoice Dollar = new("USD", "$", 1m, null, "USD");

    /// <summary>A priced turn shows its tokens and cost.</summary>
    [Test]
    public void APricedTurn_ShowsItsTokensAndCost()
    {
        Assert.That(UsageFormat.Line(Priced), Is.EqualTo("1.9k in · 26k cached · 412 out · $0.004"));
        Assert.That(UsageFormat.Total(Priced), Is.EqualTo("This chat: 3 requests · 28k tokens · $0.004"));
        Assert.That(UsageFormat.Detail(Priced), Is.EqualTo("3 requests: 1,900 input, 0 cache write, 26,000 cache read, 412 output tokens; 0.004 USD charged"));
    }

    /// <summary>An unpriced turn says unknown, never zero.</summary>
    [Test]
    public void AnUnpricedTurn_SaysUnknown_NeverZero()
    {
        Assert.That(UsageFormat.Line(Unpriced), Is.EqualTo("1.9k in · 26k cached · 412 out · price unknown"));
        Assert.That(UsageFormat.Cost(Unpriced), Is.EqualTo("price unknown"));
        Assert.That(UsageFormat.Cost(Unpriced, Rand), Is.EqualTo("price unknown"), "a currency doesn't invent a price");
        Assert.That(UsageFormat.Detail(Unpriced), Does.EndWith("; price unknown"));
        Assert.That(UsageFormat.Line(Unpriced), Does.Not.Contain("0.00"));
    }

    /// <summary>A partly priced total says how many have no price.</summary>
    [Test]
    public void APartlyPricedTotal_SaysHowManyHaveNoPrice()
    {
        var partly = new TurnUsage(100, 0, 0, 50, 2, 0.004m, "USD", 1);

        Assert.That(UsageFormat.Cost(partly), Is.EqualTo("$0.004 + 1 unpriced"));
        Assert.That(UsageFormat.Detail(partly), Does.Contain("0.004 USD charged (1 unpriced)"));
    }

    /// <summary>The display currency converts what was charged in its base.</summary>
    [Test]
    public void TheDisplayCurrency_ConvertsWhatWasChargedInItsBase()
    {
        Assert.That(UsageFormat.Line(Priced, Rand), Is.EqualTo("1.9k in · 26k cached · 412 out · R0.072"));
        Assert.That(UsageFormat.Total(Priced, Rand), Is.EqualTo("This chat: 3 requests · 28k tokens · R0.072"));
        Assert.That(UsageFormat.Detail(Priced, Rand), Does.EndWith("; 0.004 USD charged; at 18 ZAR/USD on 2026-10-13"));
        Assert.That(UsageFormat.Line(Priced, Dollar), Is.EqualTo("1.9k in · 26k cached · 412 out · $0.004"), "the base itself converts nothing");
    }

    /// <summary>A cost charged in another currency stays as charged.</summary>
    [Test]
    public void ACostChargedInAnotherCurrency_StaysAsCharged()
    {
        var euros = new TurnUsage(100, 0, 0, 50, 1, 0.5m, "EUR", 0);

        Assert.That(UsageFormat.Cost(euros, Rand), Is.EqualTo("0.5 EUR"), "not the rate's base, so no conversion; a code with no symbol goes after");
    }

    /// <summary>Money writes the symbol in front, or the code after.</summary>
    [TestCase(0, "USD", "$0")]
    [TestCase(1.5, "USD", "$1.50")]
    [TestCase(0.00123, "USD", "$0.0012")]
    [TestCase(0.066, "ZAR", "0.066 ZAR")]
    public void Money_WritesTheSymbolInFront_OrTheCodeAfter(double amount, string currency, string expected) =>
        Assert.That(UsageFormat.Money((decimal)amount, currency), Is.EqualTo(expected));

    /// <summary>Tokens shorten past a thousand.</summary>
    [TestCase(999, "999")]
    [TestCase(1_500, "1.5k")]
    [TestCase(26_000, "26k")]
    [TestCase(999_499, "999k")]
    [TestCase(1_200_000, "1.2M")]
    public void Tokens_ShortenPastAThousand(long count, string expected) =>
        Assert.That(UsageFormat.Tokens(count), Is.EqualTo(expected));

    /// <summary>A sum is null only when no part has a price.</summary>
    [Test]
    public void KnownSum_IsNullOnlyWhenNoPartHasAPrice()
    {
        Assert.That(UsageFormat.KnownSum([null, null]), Is.Null);
        Assert.That(UsageFormat.KnownSum([null, 0.5m, 0.25m]), Is.EqualTo(0.75m));
        Assert.That(UsageFormat.KnownSum([]), Is.Null);
    }

    /// <summary>Turns add up, and a cost is unknown only when neither has one.</summary>
    [Test]
    public void TurnsAddUp_AndACostIsUnknownOnlyWhenNeitherHasOne()
    {
        TurnUsage sum = Priced + Unpriced;

        Assert.That(sum.Requests, Is.EqualTo(6));
        Assert.That(sum.TotalTokens, Is.EqualTo(Priced.TotalTokens * 2));
        Assert.That(sum.Cost, Is.EqualTo(0.004m));
        Assert.That(sum.Unpriced, Is.EqualTo(3));
        Assert.That((Unpriced + Unpriced).Cost, Is.Null);
    }
}
