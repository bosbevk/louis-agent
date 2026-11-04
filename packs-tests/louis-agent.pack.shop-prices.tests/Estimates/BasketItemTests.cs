using LouisAgent.Packs.ShopPrices.Estimates;
using LouisAgent.Packs.ShopPrices.Prices;
using LouisAgent.Packs.ShopPrices.Prices.Files;

namespace LouisAgent.Packs.ShopPrices.Tests.Estimates;

/// <summary>A shopping-list line is read into its name, its pack's size and how many packs.</summary>
public class BasketItemTests
{
    /// <summary>Weights and volumes are grams and millilitres; a bare number counts items; x counts packs.</summary>
    [TestCase("beef mince 500 g", "beef mince", 500, PackUnit.Gram, 1)]
    [TestCase("beef mince 500g", "beef mince", 500, PackUnit.Gram, 1)]
    [TestCase("rice 1 kg", "rice", 1000, PackUnit.Gram, 1)]
    [TestCase("maize meal 2,5 kg", "maize meal", 2500, PackUnit.Gram, 1)]
    [TestCase("maize meal 2.5kg", "maize meal", 2500, PackUnit.Gram, 1)]
    [TestCase("sunflower oil 750 ml", "sunflower oil", 750, PackUnit.Millilitre, 1)]
    [TestCase("milk 2 l", "milk", 2000, PackUnit.Millilitre, 1)]
    [TestCase("eggs 18", "egg", 18, PackUnit.Each, 1)]
    [TestCase("6 eggs", "egg", 6, PackUnit.Each, 1)]
    [TestCase("tinned tomatoes 410 g x 2", "tinned tomato", 410, PackUnit.Gram, 2)]
    [TestCase("tinned tomatoes 410 g × 2", "tinned tomato", 410, PackUnit.Gram, 2)]
    [TestCase("2 x onions 1 kg", "onion", 1000, PackUnit.Gram, 2)]
    [TestCase("onions 1kg x2", "onion", 1000, PackUnit.Gram, 2)]
    public void ALine_IsReadIntoItsParts(string line, string name, decimal amount, PackUnit unit, int packs)
    {
        var item = BasketItem.Parse(line);

        Assert.That(string.Join(' ', item.Words), Is.EqualTo(name));
        Assert.That(item.Amount, Is.EqualTo(amount));
        Assert.That(item.Unit, Is.EqualTo(unit));
        Assert.That(item.Packs, Is.EqualTo(packs));
        Assert.That(item.Text, Is.EqualTo(line));
    }

    /// <summary>A line with no size has none, and is one pack.</summary>
    [Test]
    public void ALineWithNoSize_HasNoAmount()
    {
        var item = BasketItem.Parse("fresh coriander");

        Assert.That(item.Amount, Is.Null);
        Assert.That(item.Packs, Is.EqualTo(1));
        Assert.That(item.Words, Is.EqualTo(new[] { "fresh", "coriander" }));
    }
}
