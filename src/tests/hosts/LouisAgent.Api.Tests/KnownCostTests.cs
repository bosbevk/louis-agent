
using LouisAgent.Web.Usage;

namespace LouisAgent.Api.Tests;

/// <summary>
/// An unknown price is null, never 0 (ADR 0007): a run nobody priced once showed $0.00 everywhere. The pages sum with
/// the ledger's own rule: null only when no part has a price, else the known parts.
/// </summary>
public class KnownCostTests
{
    /// <summary>Nothing priced is unknown, not zero.</summary>
    [Test]
    public void NothingPriced_IsUnknown_NotZero() =>
        Assert.That(UsageFormat.KnownSum([null, null]), Is.Null);

    /// <summary>No parts at all is unknown too.</summary>
    [Test]
    public void NoParts_IsUnknown() =>
        Assert.That(UsageFormat.KnownSum([]), Is.Null);

    /// <summary>Some priced is the sum of those.</summary>
    [Test]
    public void SomePriced_IsTheirSum() =>
        Assert.That(UsageFormat.KnownSum([0.01m, null, 0.02m]), Is.EqualTo(0.03m));

    /// <summary>A price of zero is a known zero.</summary>
    [Test]
    public void APricedZero_IsZero() =>
        Assert.That(UsageFormat.KnownSum([0m]), Is.EqualTo(0m), "a request priced at 0 is known to be free");
}
