using LouisAgent.Core.Skills;
using LouisAgent.Identities.Puls;
using LouisAgent.Packs.Shops;

// CL15 step 3: the API's tests open Puls's sessions (puls-cook, puls/voice-af), so they register Puls's prompts as the
// Puls stack does with IDENTITIES=puls, with the shops pack Puls names (CL15 step 4), before the first prompt is read.
[SetUpFixture]
public class PulsPrompts
{
    [OneTimeSetUp]
    public void Register() => SkillFileSource.Use([new PulsIdentity().Prompts, new ShopsPack(new ShopsOptions()).Prompts]);
}
