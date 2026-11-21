using LouisAgent.Core.Skills;
using LouisAgent.Packs.WhereToBuy;

// CL15 step 4: a test assembly registers the prompts its pack brings before the first is read, as a host's Build does
// with the catalogue's.
[SetUpFixture]
public class WhereToBuyPrompts
{
    [OneTimeSetUp]
    public void Register() => SkillFileSource.Use([new WhereToBuyPack().Prompts]);
}
