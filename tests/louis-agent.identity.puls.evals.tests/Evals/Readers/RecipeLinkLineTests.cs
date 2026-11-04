using LouisAgent.Identities.Puls.Evals.Readers;

namespace LouisAgent.Identities.Puls.Tests.Evals.Readers;

/// <summary>
/// Puls's link reader finds the recipe's link the way the cook writes it, in either language and in the shapes real
/// answers have (a bare address, a markdown link, a bold label, a full stop after it), and finds nothing where the
/// answer has no such line. The answers here are shaped like the recorded cook transcripts.
/// </summary>
public class RecipeLinkLineTests
{
    /// <summary>The recipe link of an answer, read by Puls's reader.</summary>
    private static string? Link(string answer) => new RecipeLinkLine().RecipeLink(answer);

    /// <summary>An answer in the cook's shape with this line where the recipe link goes.</summary>
    private static string Answer(string linkLine) =>
        "### Recipe\n\n![Kerrie](https://a.co.za/wp-content/kerrie.jpg)\n\nServes 4 · about 1 hour\n\n"
        + "1. Fry.\n2. Simmer.\n\n"
        + linkLine
        + "\n\n### Shopping list\n\n| Ingredient | For the recipe | To buy | Estimated price |\n|---|---|---|---|\n"
        + "| Chicken | 600 g | chicken 1 kg | ± R75 |\n\n### Where to buy\n\n"
        + "| Shop | Branch | Street or mall | Website |\n|---|---|---|---|\n"
        + "| Pick n Pay | Menlyn | Atterbury Road | [pnp.co.za](https://www.pnp.co.za) |\n";

    /// <summary>The reader is listed under the name an eval file uses.</summary>
    [Test]
    public void TheReader_IsNamedRecipeLink() => Assert.That(new RecipeLinkLine().Name, Is.EqualTo("recipe-link"));

    /// <summary>The Afrikaans <c>Skakel:</c> and the English <c>Link:</c> are the recipe's link.</summary>
    [TestCase("Skakel: https://marindakook.co.za/bobotie/", "https://marindakook.co.za/bobotie/")]
    [TestCase("Link: https://af.hiloved.com/maklike-kerrie-hoender-resep/",
        "https://af.hiloved.com/maklike-kerrie-hoender-resep/")]
    [TestCase("skakel: https://thelazymakoti.com/spaghetti-bolognese/",
        "https://thelazymakoti.com/spaghetti-bolognese/")]
    public void TheLinkLine_InEitherLanguage_IsTheRecipeLink(string line, string link) =>
        Assert.That(Link(Answer(line)), Is.EqualTo(link));

    /// <summary>The link alone on a line is read to the end of the line, nothing after it.</summary>
    [Test]
    public void TheLinkAloneOnItsLine_IsReadWhole() =>
        Assert.That(Link("Skakel: https://thecinnamonjar.com/easy-south-african-bobotie/"),
            Is.EqualTo("https://thecinnamonjar.com/easy-south-african-bobotie/"));

    /// <summary>
    /// A link the model dressed: a markdown link, a bold label (the colon in or out of the bold), an angle-bracketed
    /// address, a list dash, and trailing punctuation that is the sentence's and not the address's.
    /// </summary>
    [TestCase("Skakel: [Pap en wors tert](https://yourrecipeblog.com/recipes/pap-en-wors-tert/)",
        "https://yourrecipeblog.com/recipes/pap-en-wors-tert/")]
    [TestCase("**Link:** https://thecinnamonjar.com/beef-stew/", "https://thecinnamonjar.com/beef-stew/")]
    [TestCase("**Link**: https://thecinnamonjar.com/beef-stew/", "https://thecinnamonjar.com/beef-stew/")]
    [TestCase("**Skakel:** [Bobotie](https://marindakook.co.za/bobotie/)", "https://marindakook.co.za/bobotie/")]
    [TestCase("Link: <https://thecinnamonjar.com/beef-stew/>", "https://thecinnamonjar.com/beef-stew/")]
    [TestCase("- Link: https://thecinnamonjar.com/beef-stew/", "https://thecinnamonjar.com/beef-stew/")]
    [TestCase("Link: https://thecinnamonjar.com/beef-stew/.", "https://thecinnamonjar.com/beef-stew/")]
    [TestCase("Link: https://thecinnamonjar.com/beef-stew/   ", "https://thecinnamonjar.com/beef-stew/")]
    public void ADressedLink_IsReadAsTheAddress(string line, string link) =>
        Assert.That(Link(Answer(line)), Is.EqualTo(link));

    /// <summary>A line with Windows line breaks is read as the same line.</summary>
    [Test]
    public void AnAnswerWithWindowsLineBreaks_IsReadTheSame() =>
        Assert.That(Link(Answer("Link: https://thecinnamonjar.com/beef-stew/").Replace("\n", "\r\n")),
            Is.EqualTo("https://thecinnamonjar.com/beef-stew/"));

    /// <summary>
    /// The first such line is the recipe's: a later <c>Link:</c> line, under Where to buy say, is not the recipe link.
    /// </summary>
    [Test]
    public void AnotherLinkLineLater_IsNotTheRecipeLink()
    {
        string answer = Answer("Link: https://thecinnamonjar.com/beef-stew/")
                        + "\nLink: https://www.pnp.co.za/store-locator\n";

        Assert.That(Link(answer), Is.EqualTo("https://thecinnamonjar.com/beef-stew/"));
    }

    /// <summary>
    /// The word <i>link</i> inside a sentence is no label, and neither are words that only begin with it (a share
    /// button's <c>Linkedin</c>).
    /// </summary>
    [TestCase("Use the link: https://thecinnamonjar.com/beef-stew/ for the method.")]
    [TestCase("The recipe link: https://thecinnamonjar.com/beef-stew/")]
    [TestCase("Linkedin: https://www.linkedin.com/shareArticle?url=x")]
    [TestCase("Link to the page: https://thecinnamonjar.com/beef-stew/")]
    public void ALinkWordInASentence_IsNotALinkLine(string line) => Assert.That(Link(Answer(line)), Is.Null);

    /// <summary>A label with no address after it, or one that is no web address, gives none.</summary>
    [TestCase("Link:")]
    [TestCase("Skakel: ")]
    [TestCase("Link: none")]
    [TestCase("Link: [the page](/recipes/beef-stew/)")]
    [TestCase("Link: ftp://thecinnamonjar.com/beef-stew/")]
    public void ALabelWithNoWebAddress_GivesNone(string line) => Assert.That(Link(Answer(line)), Is.Null);

    /// <summary>
    /// An answer with an image link and a price link but no <c>Link:</c> line gives none: the photo above the
    /// method and a shop's price are not the recipe's page.
    /// </summary>
    [Test]
    public void AnImageLinkAndAPriceLink_AreNotTheRecipeLink()
    {
        const string answer =
            "### Recipe\n\n![Bobotie](https://marindakook.co.za/wp-content/bobotie.jpg)\n\nServes 4\n\n"
            + "| Ingredient | To buy | Price |\n|---|---|---|\n"
            + "| Mince | mince 500 g | [R47,00](https://www.pnp.co.za/p/1) |\n";

        Assert.That(Link(answer), Is.Null);
    }

    /// <summary>An image on the label's line is the photo, not the page.</summary>
    [Test]
    public void ALabelBeforeAnImage_IsNotTheRecipeLink() =>
        Assert.That(Link("Link: ![Bobotie](https://marindakook.co.za/wp-content/bobotie.jpg)"), Is.Null);

    /// <summary>An answer with no recipe link at all gives none.</summary>
    [TestCase("")]
    [TestCase("Bobotie, vir vier mense.")]
    public void AnAnswerWithNoLink_GivesNone(string answer) => Assert.That(Link(answer), Is.Null);
}
