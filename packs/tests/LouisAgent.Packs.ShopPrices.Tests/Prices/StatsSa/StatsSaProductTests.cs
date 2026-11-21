using LouisAgent.Packs.ShopPrices.Prices.StatsSa;

namespace LouisAgent.Packs.ShopPrices.Tests.Prices.StatsSa;

/// <summary>A product's name tidied the way the shipped file has it: one space, and a dash with a space each side.</summary>
public class StatsSaProductTests
{
    [TestCase("Onions  - fresh", "Onions - fresh")]
    [TestCase("Fizzy drinks- can", "Fizzy drinks - can")]
    [TestCase("Hake -Fresh Or Frozen", "Hake - Fresh Or Frozen")]
    [TestCase("Sunflower-seed  oil (cooking oil)", "Sunflower - seed oil (cooking oil)")]
    [TestCase("Beef T-bone", "Beef T - bone")]
    [TestCase(" White Rice ", "White Rice")]
    public void AName_IsTidied(string name, string tidy) =>
        Assert.That(StatsSaProduct.Tidy(name), Is.EqualTo(tidy));
}
