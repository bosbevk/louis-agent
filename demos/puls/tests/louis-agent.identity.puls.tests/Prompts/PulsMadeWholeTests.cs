using LouisAgent.Core.Skills.Headers;

namespace LouisAgent.Identities.Puls.Tests.Prompts;

/// <summary>
/// A cake is per cake: nobody cuts a recipe down for the people eating it. So Puls doesn't ask how many, or where the
/// user is, and the cook keeps the recipe as it's written and says what it makes.
/// </summary>
public class PulsMadeWholeTests
{
    /// <summary>A prompt's file as the model reads it, through the catalogue, on one line.</summary>
    private static string Prompt(string path) =>
        string.Join(' ', PromptCatalog.Get(path)!.Body.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    /// <summary>Puls hands a cake straight on, with no number, and asks for no town.</summary>
    [Test]
    public void Puls_NeverAsksHowMany_ForSomethingMadeWhole()
    {
        string router = Prompt("agent/puls/voice.md");

        Assert.That(router, Does.Contain("Never ask how many for something made whole"));
        Assert.That(router, Does.Contain("a cake, a tart, a pudding, a loaf, rusks"));
        Assert.That(router, Does.Contain("leave the line out"));
        Assert.That(router, Does.Contain("Never ask where they are"));
    }

    /// <summary>With no people in the task, the recipe stays as written and the report says what it makes.</summary>
    [Test]
    public void TheCook_KeepsSomethingMadeWhole_AsItIsWritten()
    {
        string cook = Prompt("specialists/puls-cook.md");

        Assert.That(cook, Does.Contain("With no such line it's made whole: keep the recipe as it is, a factor of 1"));
        Assert.That(cook, Does.Contain("*Maak 1 koek · ongeveer 1 uur*"));
        Assert.That(cook, Does.Contain("leave `serves` out"));
    }
}
