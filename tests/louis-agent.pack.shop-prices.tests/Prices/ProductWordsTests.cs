using LouisAgent.Packs.ShopPrices.Prices;

namespace LouisAgent.Packs.ShopPrices.Tests.Prices;

/// <summary>A name's words meet another's whatever the case, the punctuation or the plural.</summary>
public class ProductWordsTests
{
    /// <summary>Stats SA's way of writing a name and a shopping list's meet on the same words.</summary>
    [TestCase("Onions  - fresh", new[] { "onion", "fresh" })]
    [TestCase("ONIONS", new[] { "onion" })]
    [TestCase("Tomatoes", new[] { "tomato" })]
    [TestCase("cherries", new[] { "cherry" })]
    [TestCase("Sunflower-seed oil (cooking oil)", new[] { "sunflower", "seed", "oil", "cooking", "oil" })]
    [TestCase("glass", new[] { "glass" })]
    [TestCase("gas", new[] { "gas" })]
    public void AName_IsItsSingularLowerCaseWords(string name, string[] words) =>
        Assert.That(ProductWords.Of(name), Is.EqualTo(words));
}
