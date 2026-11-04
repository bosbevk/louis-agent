using LouisAgent.Packs.WhereToBuy.Branches;

namespace LouisAgent.Packs.WhereToBuy.Tests.Branches;

/// <summary>A shop is searched by the name the map uses, and a branch matches any of its names.</summary>
public class ShopNamesTests
{
    /// <summary>The short names are searched as the full one; a shop not listed keeps its own name.</summary>
    [TestCase("PnP", "Pick n Pay")]
    [TestCase("pick 'n pay", "Pick n Pay")]
    [TestCase("Woolies", "Woolworths")]
    [TestCase(" Spar ", "Spar")]
    public void TheSearchName_IsTheMapsName(string shop, string search) =>
        Assert.That(ShopNames.Of(shop)[0], Is.EqualTo(search));

    /// <summary>A branch matches by any of the shop's names, whatever its case or punctuation.</summary>
    [TestCase("PnP Express", true)]
    [TestCase("Pick 'n Pay Family", true)]
    [TestCase("Checkers Hyper", false)]
    [TestCase(null, false)]
    public void ABranch_MatchesByAnyOfItsNames(string? place, bool matches) =>
        Assert.That(ShopNames.IsBranch(place, ShopNames.Of("Pick n Pay")), Is.EqualTo(matches));
}
