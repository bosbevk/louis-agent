using LouisAgent.Core.Skills;
using LouisAgent.Packs.ShopPrices;

/// <summary>
/// Registers the pack's prompts before the first is read, as a host's Build does with the catalogue's.
/// </summary>
[SetUpFixture]
public class ShopPricesPrompts
{
    /// <summary>The pack's guide, read through the one prompt reader.</summary>
    [OneTimeSetUp]
    public void Register() => SkillFileSource.Use([new ShopPricesPack().Prompts]);
}
