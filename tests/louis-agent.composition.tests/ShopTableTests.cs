using LouisAgent.Identities.Puls.Evals.Checks.Shared.Scoring;
using LouisAgent.Packs.Shops.Checks.Reading;

namespace LouisAgent.Composition.Tests;

/// <summary>
/// The shops' names and sites live twice: in the shops pack, for its shop rules, and in Puls, for its price reading,
/// since an identity may not reference a pack (ADR 0027). This is the one project that sees both, so it holds them
/// equal: a shop added to one and not the other fails here.
/// </summary>
/// <remarks>Louis's pick (F49.3 step 1b): two copies pinned by a test, over moving Puls's prices to the pack.</remarks>
public class ShopTableTests
{
    /// <summary>Both copies name the same shops, with the same sites, in the same order.</summary>
    [Test]
    public void PulssShopTable_IsThePacks() =>
        Assert.That(Shops.Hosts, Is.EqualTo(ShopSites.Hosts));
}
