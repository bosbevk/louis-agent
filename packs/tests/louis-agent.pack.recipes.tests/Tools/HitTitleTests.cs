using LouisAgent.Packs.Recipes.Tools;

namespace LouisAgent.Packs.Recipes.Tests.Tools;

/// <summary>
/// Every site in our table is South African, so a title saying so tells the cook nothing, yet it read those pages about
/// twice as often as chance. The list shows the dish instead.
/// </summary>
public class HitTitleTests
{
    /// <summary>The country goes, in either language and any case, with what only led up to it.</summary>
    [TestCase("South African Potjie: Beef stew with dumplings", "Potjie: Beef stew with dumplings")]
    [TestCase("Traditional South African Malva Pudding", "Traditional Malva Pudding")]
    [TestCase("TRADITIONAL SOUTH AFRICAN BOBOTIE", "TRADITIONAL BOBOTIE")]
    [TestCase("The Best Koeksister Recipe in South Africa", "The Best Koeksister Recipe")]
    [TestCase("South Africa's best bobotie", "best bobotie")]
    [TestCase("South Africa’s best bobotie", "best bobotie")]
    [TestCase("Tradisionele Suid-Afrikaanse melktert", "Tradisionele melktert")]
    [TestCase("Bobotie - South African", "Bobotie")]
    [TestCase("Proudly South African Malva Pudding", "Malva Pudding")]
    public void TheCountry_ComesOutOfTheTitle(string title, string shown) =>
        Assert.That(HitTitle.Shown(title), Is.EqualTo(shown));

    /// <summary>A title that doesn't say it stays exactly as it is.</summary>
    [TestCase("Chicken Potjie Recipe")]
    [TestCase("Boerewors, Cheese & Caramelized Onion Sausage Rolls")]
    public void AnyOtherTitle_StaysAsItIs(string title) =>
        Assert.That(HitTitle.Shown(title), Is.EqualTo(title));

    /// <summary>A title that is nothing but the country keeps it: a hit needs a name.</summary>
    [Test]
    public void ATitleThatIsOnlyTheCountry_StaysAsItIs() =>
        Assert.That(HitTitle.Shown("South African"), Is.EqualTo("South African"));
}
