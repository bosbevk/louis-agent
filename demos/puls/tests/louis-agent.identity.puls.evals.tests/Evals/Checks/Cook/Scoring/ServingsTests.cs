using LouisAgent.Identities.Puls.Evals.Checks.Cook.Scoring;

namespace LouisAgent.Identities.Puls.Tests.Evals.Checks.Cook.Scoring;

/// <summary>
/// How many people an answer says it serves, in Afrikaans and English, in digits or words.
/// </summary>
public class ServingsTests
{
    /// <summary>The number of people is read in either language, as a digit or a word.</summary>
    [TestCase("Hier is bobotie vir 4 mense.", new[] { 4 })]
    [TestCase("Hier is bobotie vir vier mense.", new[] { 4 })]
    [TestCase("Dit voed 4 mense met 'n goeie porsie.", new[] { 4 })]
    [TestCase("Maak dit vir 6 persone.", new[] { 6 })]
    [TestCase("Resep vir twee.", new[] { 2 })]
    [TestCase("Recipe for 4.", new[] { 4 })]
    [TestCase("Serves 4, with rice.", new[] { 4 })]
    [TestCase("This feeds six people.", new[] { 6 })]
    [TestCase("A stew for two, ready in an hour.", new[] { 2 })]
    [TestCase("8 portions", new[] { 8 })]
    public void ItReadsTheNumberOfPeople(string answer, int[] expected) =>
        Assert.That(Servings.Of(answer), Is.EqualTo(expected));

    /// <summary>Every number of people is read, once each, in the order the answer first says them.</summary>
    [Test]
    public void ItReadsEveryNumberTheAnswerNames_InTheOrderItFirstSaysThem() =>
        Assert.That(Servings.Of("Vir 4 mense, of vir ses mense as julle honger is. Vir 4 mense: R120."),
            Is.EqualTo(new[] { 4, 6 }));

    /// <summary>Times, quantities and prices are never mistaken for people.</summary>
    [TestCase("Bak vir 45 minute by 180 grade.")]
    [TestCase("Bake for 45 minutes, then rest for 2 hours.")]
    [TestCase("Kook vir 2 uur.")]
    [TestCase("Add 500 g mince and 2 onions.")]
    [TestCase("Total: R149,95.")]
    [TestCase("")]
    public void ItReadsNothingFromTimesQuantitiesAndPrices(string answer) =>
        Assert.That(Servings.Of(answer), Is.Empty);

    /// <summary>No answer names nobody.</summary>
    [Test]
    public void ANullAnswerNamesNobody() => Assert.That(Servings.Of(null), Is.Empty);
}
