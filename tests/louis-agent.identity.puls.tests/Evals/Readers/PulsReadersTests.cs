using LouisAgent.Identities.Puls.Evals;
using LouisAgent.Identities.Puls.Evals.Readers;

namespace LouisAgent.Identities.Puls.Tests.Evals.Readers;

/// <summary>
/// Puls's readers, the ones its eval file names beside the general checks: each reads what the cook's own check read,
/// with the same scoring code, so swapping the check for the general one changes no grade.
/// </summary>
public class PulsReadersTests
{
    /// <summary>Puls lists its readers under the names its eval file uses.</summary>
    [Test]
    public void PulssReaders_AreListedByName() =>
        Assert.That(PulsReaders.All.Select(reader => reader.Name),
            Is.EqualTo(new[] { "rand-total", "servings", "recipe-photos" }));

    /// <summary>
    /// The rand total is the basket's, from its total line or the bold total row of a list, in either language.
    /// </summary>
    [TestCase("Hier is die lys.\nTotaal: R149,95", 149.95)]
    [TestCase("| **Total** | | | **± R112** |\n| **Per person** | | | **± R28** |\n", 112)]
    public void TheRandTotal_IsTheBasketsTotal(string answer, decimal total) =>
        Assert.That(new RandTotal().Total(answer), Is.EqualTo(total));

    /// <summary>An answer with no total gives none.</summary>
    [Test]
    public void NoTotal_IsNone() => Assert.That(new RandTotal().Total("Ek kon nie pryse vind nie."), Is.Null);

    /// <summary>The people served are read in digits or in words, Afrikaans or English.</summary>
    [TestCase("Hier is bobotie vir vier mense.", 4)]
    [TestCase("A stew for two.", 2)]
    [TestCase("Bobotie vir 6 mense.", 6)]
    public void ThePeopleServed_AreReadInDigitsOrWords(string answer, int people) =>
        Assert.That(new PeopleServed().Numbers(answer), Does.Contain(people));

    /// <summary>A recipe photo is an image of the text, but a share button, a print icon or a logo is none.</summary>
    [Test]
    public void ARecipePhoto_IsAnImage_ButAPagesFurnitureIsNone() =>
        Assert.That(new RecipePhotos().Images(
                "![Bobotie](https://a.co.za/wp-content/bobotie.jpg) ![x](https://a.co.za/images/logo.png) " +
                "![x](https://a.co.za/wp-content/plugins/pdf-print/images/pdf.png)"),
            Is.EqualTo(new[] { "https://a.co.za/wp-content/bobotie.jpg" }));
}
