using LouisAgent.Core.Skills.Headers;

namespace LouisAgent.Identities.Puls.Tests.Prompts;

/// <summary>
/// A recipe or a meal. Someone asking for a recipe wants the recipe, as the page wrote it, not a question back; a number
/// only matters when they name one or want a meal planned. And a cake is per cake: more people means another cake.
/// </summary>
public class PulsMadeWholeTests
{
    /// <summary>A prompt's file as the model reads it, through the catalogue, on one line.</summary>
    private static string Prompt(string path) =>
        string.Join(' ', PromptCatalog.Get(path)!.Body.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    /// <summary>A recipe or a named dish goes straight on, with no number; only a meal to plan is asked about.</summary>
    [Test]
    public void Puls_GivesARecipe_WithoutAskingHowMany()
    {
        string router = Prompt("agent/puls/voice.md");

        Assert.That(router, Does.Contain("Someone who asks for a recipe or names a dish gets it: never ask how many"));
        Assert.That(router, Does.Contain("a meal to plan that doesn't say for how many (*what's for dinner?*)"));
        Assert.That(router, Does.Contain("leave the line out"));
        Assert.That(router, Does.Contain("Never ask where they are"));
    }

    /// <summary>A budget per person can't be worked out without the people, so that still asks.</summary>
    [Test]
    public void ABudgetPerPerson_StillAsksHowMany() =>
        Assert.That(Prompt("agent/puls/voice.md"), Does.Contain("a budget per person with no number"));

    /// <summary>What Puls hands on keeps the user's own words for the dish: "butternut" became butter beans once.</summary>
    [Test]
    public void TheBrief_KeepsTheDishInTheUsersOwnWords()
    {
        Assert.That(Prompt("agent/puls/voice.md"), Does.Contain("names the dish in the user's own words, never translated"));
        Assert.That(Prompt("specialists/puls-cook.md"), Does.Contain("search for the dish in the words the task names it"));
    }

    /// <summary>The line before handing on is in the language the user wrote, whatever mode the chat is in.</summary>
    [Test]
    public void TheHandOverLine_IsInTheUsersLanguage() =>
        Assert.That(Prompt("agent/puls/voice.md"), Does.Contain("Say one line to the user first, in the language they wrote"));

    /// <summary>With no people in the task, the recipe stays as written, no tool, and says what it makes.</summary>
    [Test]
    public void TheCook_KeepsARecipeAsItIsWritten_WithNoPeople()
    {
        string cook = Prompt("specialists/puls-cook.md");

        Assert.That(cook, Does.Contain("With no such line, give the recipe as the page wrote it, a factor of 1, with no `ScaleRecipe`"));
        Assert.That(cook, Does.Contain("*Maak 1 koek · ongeveer 1 uur*"));
        Assert.That(cook, Does.Contain("leave `serves` out"));
    }

    /// <summary>Something made whole comes in whole ones, rounded up, as running short is worse than leftovers.</summary>
    [Test]
    public void SomethingMadeWhole_ComesInWholeOnes_RoundedUp()
    {
        string cook = Prompt("specialists/puls-cook.md");

        Assert.That(cook, Does.Contain("Something made whole (a cake, a tart, a loaf, a whole roast or chicken) comes in whole ones"));
        Assert.That(cook, Does.Contain("rounded up, as running short is worse than leftovers"));
        Assert.That(cook, Does.Contain("a stew or a batch of biscuits scales as usual"));
    }

    /// <summary>When someone names a number the tool's lines stand: a fifth of a batch of samoosas came back whole once.</summary>
    [Test]
    public void TheCook_DoesntOverrideTheTool() =>
        Assert.That(Prompt("specialists/puls-cook.md"), Does.Contain("don't go back to the page's amounts"));
}
