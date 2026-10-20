using LouisAgent.Core.Skills;
using LouisAgent.Identities.Puls;
using LouisAgent.Packs.Shops;

// CL15: a test assembly registers the prompts its identity, and the pack it names (step 4), bring before the first is
// read, as a host's Build does with the catalogue's.
[SetUpFixture]
public class PulsPrompts
{
    [OneTimeSetUp]
    public void Register() => SkillFileSource.Use([new PulsIdentity().Prompts, new ShopsPack(new ShopsOptions()).Prompts]);
}
