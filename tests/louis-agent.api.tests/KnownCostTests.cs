using LouisAgent.Web.Shared.Services;

namespace LouisAgent.Api.Tests;

// F31 review of PR #55: the explorer summed a run's costs with `Cost ?? 0`, so a run nobody priced (the stub's) showed
// $0.00 everywhere. ADR 0007: an unknown price is null, never 0. The pages sum with the ledger's own rule
// (UsageReport.Total): null only when no part has a price, else the known parts.
public class KnownCostTests
{
    [Test]
    public void NothingPriced_IsUnknown_NotZero() =>
        Assert.That(UsageFormat.KnownSum([null, null]), Is.Null);

    [Test]
    public void NoParts_IsUnknown() =>
        Assert.That(UsageFormat.KnownSum([]), Is.Null);

    [Test]
    public void SomePriced_IsTheirSum() =>
        Assert.That(UsageFormat.KnownSum([0.01m, null, 0.02m]), Is.EqualTo(0.03m));

    [Test]
    public void APricedZero_IsZero() =>
        Assert.That(UsageFormat.KnownSum([0m]), Is.EqualTo(0m), "a request priced at 0 is known to be free");
}
