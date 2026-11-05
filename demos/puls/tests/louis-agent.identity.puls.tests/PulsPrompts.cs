using LouisAgent.Packs.Web;
using LouisAgent.Core.Skills;
using LouisAgent.Identities.Puls;
using LouisAgent.Packs.Calculator;
using LouisAgent.Packs.ShopPrices;
using LouisAgent.Packs.WhereToBuy;

// CL15: a test assembly registers the prompts its identity, and the packs it names (F50.2), bring before the first is
// read, as a host's Build does with the catalogue's.
[SetUpFixture]
public class PulsPrompts
{
    [OneTimeSetUp]
    public void Register() => SkillFileSource.Use([new WebPack().Prompts, new PulsIdentity().Prompts, new WhereToBuyPack().Prompts,
        new ShopPricesPack(new ShopPricesOptions()).Prompts, new CalculatorPack().Prompts]);
}
