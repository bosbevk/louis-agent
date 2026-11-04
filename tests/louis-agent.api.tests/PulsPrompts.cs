using LouisAgent.Core.Skills;
using LouisAgent.Identities.Puls;
using LouisAgent.Packs.Recipes;
using LouisAgent.Packs.Shops;

/// <summary>
/// The API's tests open Puls's sessions (puls-cook, puls/voice-af), so they register Puls's prompts as the Puls stack
/// does with IDENTITIES=puls, with the shops and recipes packs Puls names, before the first prompt is read.
/// </summary>
[SetUpFixture]
public class PulsPrompts
{
    /// <summary>Registers Puls's prompts and the packs' before any test reads one.</summary>
    [OneTimeSetUp]
    public void Register() => SkillFileSource.Use(
    [
        new PulsIdentity().Prompts,
        new ShopsPack(new ShopsOptions()).Prompts,
        new RecipesPack(new RecipesOptions()).Prompts,
    ]);
}
