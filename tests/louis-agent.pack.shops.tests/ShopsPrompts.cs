using LouisAgent.Core.Skills;
using LouisAgent.Packs.Shops;

// CL15 step 4: a test assembly registers the prompts its pack brings before the first is read, as a host's Build does
// with the catalogue's.
[SetUpFixture]
public class ShopsPrompts
{
    [OneTimeSetUp]
    public void Register() => SkillFileSource.Use([new ShopsPack(new ShopsOptions()).Prompts]);
}
