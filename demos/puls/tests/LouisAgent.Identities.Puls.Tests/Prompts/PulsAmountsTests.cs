using LouisAgent.Core.Skills.Headers;

namespace LouisAgent.Identities.Puls.Tests.Prompts;

/// <summary>
/// What the cook does with amounts that have nowhere obvious to go. Each rule comes from a fail in the runs: oil given
/// "2 eetlepels" when the page gave none, a plastic bag on the shopping list, and a dish's water left unscaled because
/// water has no row.
/// </summary>
public class PulsAmountsTests
{
    /// <summary>The cook's file as the model reads it, through the catalogue, on one line.</summary>
    private static string Cook() =>
        string.Join(' ', PromptCatalog.Get("specialists/puls-cook.md")!.Body.Split((char[]?)null,
            StringSplitOptions.RemoveEmptyEntries));

    /// <summary>The cook scales with the tool, copies its lines, and does only what the tool marks.</summary>
    [Test]
    public void TheCook_ScalesWithTheTool_AndCopiesItsLines()
    {
        string cook = Cook();

        Assert.That(cook, Does.Contain("scale it with `ScaleRecipe`"));
        Assert.That(cook, Does.Contain("use its lines as they are"));
        Assert.That(cook, Does.Contain("A line it marks *(not scaled)* is yours to work out with `Calculate`"));
        Assert.That(cook, Does.Contain("*(only the first amount scaled)*"));
        Assert.That(cook, Does.Contain("a pack's size stays (`2 x 400 g`)"));
        Assert.That(cook, Does.Contain("On a page it says it doesn't hold"));
    }

    /// <summary>A page with no yield still goes to the tool, and its estimate is said under *Unsure*.</summary>
    [Test]
    public void APageWithNoYield_StillGoesToTheTool() =>
        Assert.That(Cook(), Does.Contain("A print that gives neither still goes to `ScaleRecipe`"));

    /// <summary>An ingredient used twice shows both amounts and their sum: the sum isn't read as a third.</summary>
    [Test]
    public void AnIngredientUsedTwice_ShowsItsSum() =>
        Assert.That(Cook(), Does.Contain("`250 ml + 1 koppie = 500 ml`"));

    /// <summary>A line with no amount keeps the page's words or a dash, never an amount of the cook's.</summary>
    [Test]
    public void ALineWithNoAmount_GetsNoAmountOfTheCooks()
    {
        string cook = Cook();

        Assert.That(cook, Does.Contain("keeps the print's words in *For the recipe* (*for frying*), or a dash"));
        Assert.That(cook, Does.Contain("never an amount of your own"));
    }

    /// <summary>Equipment a page lists among its ingredients is no row.</summary>
    [Test]
    public void WhatIsntFood_IsNoRow() =>
        Assert.That(Cook(), Does.Contain("Something the print lists that isn't food (a plastic bag, baking paper) is no row"));

    /// <summary>Water has no row, so its scaled amount goes in the step that uses it.</summary>
    [Test]
    public void WatersScaledAmount_GoesInTheStep() =>
        Assert.That(Cook(), Does.Contain("scale its amount with the rest and write it in the step that uses it"));

    /// <summary>A size stays and the count changes: half the dough is half as many 150 g portions.</summary>
    [Test]
    public void APiecesSize_DoesntScale() =>
        Assert.That(Cook(), Does.Contain("The size of one piece doesn't scale (150 g portions, a 20 cm tin)"));

    /// <summary>The print says "Serves" for a bare number, so the prompt no longer calls one "not people".</summary>
    [Test]
    public void ABareNumber_IsNoLongerCalledNotPeople() =>
        Assert.That(Cook(), Does.Not.Contain("or is a bare number"));
}
