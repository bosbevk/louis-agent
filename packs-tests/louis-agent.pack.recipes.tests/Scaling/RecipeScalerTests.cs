using LouisAgent.Packs.Recipes.Scaling;

namespace LouisAgent.Packs.Recipes.Tests.Scaling;

/// <summary>
/// The sums a cook kept getting wrong, done in code: each line as real recipe sites write it, the amount scaled and
/// everything else left as the page has it.
/// </summary>
public class RecipeScalerTests
{
    private const double FourFromThree = 4.0 / 3;

    /// <summary>Weights and volumes come out as plain numbers, the unit stays as written.</summary>
    [TestCase("500 g beef mince", 1.5, "750 g beef mince")]
    [TestCase("250g button mushrooms", 2, "500g button mushrooms")]
    [TestCase("1,5 kg chicken", 2, "3 kg chicken")]
    [TestCase("1 kg mince", FourFromThree, "1.33 kg mince")]
    [TestCase("15 ml sugar", 0.5, "7.5 ml sugar")]
    [TestCase("1250 ml flour", 0.5, "625 ml flour")]
    public void WeightsAndVolumes_ArePlainNumbers(string line, double factor, string scaled) =>
        Assert.That(RecipeAmount.Scale(line, factor), Is.EqualTo(scaled));

    /// <summary>Spoons and cups come out as fractions a cook can measure, however the page wrote them.</summary>
    [TestCase("1 cup flour", FourFromThree, "1⅓ cup flour")]
    [TestCase("½ tsp salt", 2, "1 tsp salt")]
    [TestCase("1 ½ cups milk", 2, "3 cups milk")]
    [TestCase("1½ cups milk", 0.5, "¾ cups milk")]
    [TestCase("1/2 cup raisins", 2, "1 cup raisins")]
    [TestCase("1 1/2 tbsp oil", 2, "3 tbsp oil")]
    [TestCase("7 tablespoons water", 2.5, "17½ tablespoons water")]
    [TestCase("2 cups water", 1.5, "3 cups water")]
    public void SpoonsAndCups_AreFractions(string line, double factor, string scaled) =>
        Assert.That(RecipeAmount.Scale(line, factor), Is.EqualTo(scaled));

    /// <summary>What can be split gets a fraction, and what can't, an egg or a roll, a whole one.</summary>
    [TestCase("1 large onion", FourFromThree, "1⅓ large onion")]
    [TestCase("3 eggs", FourFromThree, "4 eggs")]
    [TestCase("5 large eggs", 0.5, "3 large eggs")]
    [TestCase("1 egg", 0.25, "1 egg")]
    [TestCase("2 eiers", 1.5, "3 eiers")]
    [TestCase("6 bread rolls", 0.75, "5 bread rolls")]
    public void WhatCantBeSplit_IsWhole(string line, double factor, string scaled) =>
        Assert.That(RecipeAmount.Scale(line, factor), Is.EqualTo(scaled));

    /// <summary>Both ends of a range scale.</summary>
    [TestCase("2-3 carrots", 2, "4-6 carrots")]
    [TestCase("1 to 2 cups stock", 2, "2 to 4 cups stock")]
    [TestCase("10–12 dried apricots", 0.5, "5–6 dried apricots")]
    public void BothEndsOfARange_Scale(string line, double factor, string scaled) =>
        Assert.That(RecipeAmount.Scale(line, factor), Is.EqualTo(scaled));

    /// <summary>
    /// A second measure in brackets is the same amount, so it scales too, but a pack's own size doesn't: two packets of
    /// 10 g are still 10 g each.
    /// </summary>
    [TestCase("125 ml (1/2 cup) dry white wine", 2, "250 ml (1 cup) dry white wine")]
    [TestCase("52 ml (50 g) botter", 2, "104 ml (100 g) botter")]
    [TestCase("1 packet (10g) instant yeast", 2, "2 packet (10g) instant yeast")]
    [TestCase("1 x 400g roll puff pastry", 2, "2 x 400g roll puff pastry")]
    public void ABracketsMeasure_ScalesButAPacksSizeDoesnt(string line, double factor, string scaled) =>
        Assert.That(RecipeAmount.Scale(line, factor), Is.EqualTo(scaled));

    /// <summary>A word that only hedges the amount stays, and the amount after it scales.</summary>
    [TestCase("about 200g of puff pastry", 2, "about 400g of puff pastry")]
    [TestCase("Approx. 2 cups lukewarm water", 1.5, "Approx. 3 cups lukewarm water")]
    [TestCase("roughly 1 kg lamb", 2, "roughly 2 kg lamb")]
    public void AHedgingWord_StaysAndTheAmountScales(string line, double factor, string scaled) =>
        Assert.That(RecipeAmount.Scale(line, factor), Is.EqualTo(scaled));

    /// <summary>
    /// A line that holds an amount this can't scale has to be told apart from one with none, or the cook would copy it
    /// unscaled, the very slip this is for.
    /// </summary>
    [TestCase("Zest of 1 orange", true)]
    [TestCase("between 1.2 and 1.5 kg of chicken", true)]
    [TestCase("one packet (15 g) seasoning", true)]
    [TestCase("two onions", true)]
    [TestCase("salt and pepper", false)]
    [TestCase("oil, for frying", false)]
    [TestCase("a pinch of nutmeg", false)]
    public void ALineWithAnAmount_IsKnownEvenIfItCantBeScaled(string line, bool hasOne) =>
        Assert.That(RecipeAmount.HoldsAnAmount(line), Is.EqualTo(hasOne));

    /// <summary>A pot's size in a step is no amount of food.</summary>
    [Test]
    public void APotsSize_IsNoAmount() =>
        Assert.That(RecipeAmount.InStep("Use a 2-litre pot and add 1 cup milk.", 2), Is.EqualTo(new[] { ("1 cup", "2 cup") }));

    /// <summary>A line with no number to scale stays as it is, and so does every line at a factor of one.</summary>
    [TestCase("salt and pepper", 2)]
    [TestCase("a pinch of nutmeg", 2)]
    [TestCase("oil, for frying", 2)]
    [TestCase("between 1.2 and 1.5 kg of chicken", 2)]
    [TestCase("1 ½ cups milk", 1)]
    public void ALineWithNothingToScale_StaysAsItIs(string line, double factor) =>
        Assert.That(RecipeAmount.Scale(line, factor), Is.EqualTo(line));

    /// <summary>How many people a yield is for: a range's middle, and nothing when it counts something else.</summary>
    [TestCase("3", 3d)]
    [TestCase("4 to 6", 5d)]
    [TestCase("6-8", 7d)]
    [TestCase("6 servings", 6d)]
    [TestCase("8-10 servings", 9d)]
    [TestCase("4 people", 4d)]
    [TestCase("6 porsies", 6d)]
    [TestCase("10 pies", null)]
    [TestCase("4 dozen", null)]
    [TestCase("12 glasies", null)]
    [TestCase(null, null)]
    public void TheYield_SaysHowManyPeopleOrNothing(string? yield, double? people) =>
        Assert.That(RecipeAmount.PeopleIn(yield), Is.EqualTo(people));

    /// <summary>Amounts of food in a step are found, and times, temperatures and sizes are left alone.</summary>
    [Test]
    public void AStepsAmounts_AreFound_NotItsTimesOrTemperatures()
    {
        var found = RecipeAmount.InStep("Soak in 1 cup milk for 10 minutes at 180 C, add 250 ml water, use a 20 cm tin.", 2);

        Assert.That(found, Is.EqualTo(new[] { ("1 cup", "2 cup"), ("250 ml", "500 ml") }));
    }

    /// <summary>A step with no amount of food in it has nothing to report.</summary>
    [Test]
    public void AStepWithNoAmount_HasNothingToReport() =>
        Assert.That(RecipeAmount.InStep("Bake for 40 minutes at 180 C.", 2), Is.Empty);
}
