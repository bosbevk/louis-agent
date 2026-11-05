using LouisAgent.Packs.Recipes.Providers;

namespace LouisAgent.Packs.Recipes.Tests.Providers;

/// <summary>
/// Three sites answer a search with no results by listing suggestions, and those read as hits: Jan Braai gave
/// <c>Puttanesca</c> for "pap and sous". A hit that shares no word with the search isn't an answer to it.
/// </summary>
public class QueryWordsTests
{
    /// <summary>The page's address for a title, with nothing useful in it.</summary>
    private static readonly Uri Plain = new("https://example.com/recipes/12345");

    /// <summary>A title holding a word of the search counts, whatever its case or its other words.</summary>
    [TestCase("Pork bobotie", "bobotie")]
    [TestCase("TRADITIONAL BOBOTIE", "bobotie")]
    [TestCase("A simple Stywe Pap recipe", "pap en sous")]
    [TestCase("Orange Chicken Marinade", "braai chicken marinade")]
    public void ATitleWithAWordOfTheSearch_IsAnAnswer(string title, string query) =>
        Assert.That(QueryWords.Answers(title, Plain, query), Is.True);

    /// <summary>The suggestions those sites really gave aren't answers.</summary>
    [TestCase("Puttanesca", "pap and sous")]
    [TestCase("Spinach lasagne", "pap and sous")]
    [TestCase("Cape Malay Pickled Fish (Ingelegde vis)", "braai chicken marinade")]
    [TestCase("Biltong Braaibroodjie", "zzqxvkw")]
    public void ASuggestion_IsNoAnswer(string title, string query) =>
        Assert.That(QueryWords.Answers(title, Plain, query), Is.False);

    /// <summary>A word that only joins others, or says where a dish is from, matches nothing on its own.</summary>
    [TestCase("Bread and butter", "pap and sous")]
    [TestCase("The best South African lamb", "South African pap")]
    [TestCase("Vis met rys", "pap met sous")]
    public void AWordThatOnlyJoinsOthers_MatchesNothing(string title, string query) =>
        Assert.That(QueryWords.Answers(title, Plain, query), Is.False);

    /// <summary>A plural, or a dish's longer name, is still the word: potjie is a potjiekos.</summary>
    [TestCase("Koeksisters", "koeksister")]
    [TestCase("Koeksister", "koeksisters")]
    [TestCase("Lamb potjie", "potjiekos")]
    public void APluralOrALongerName_IsTheWord(string title, string query) =>
        Assert.That(QueryWords.Answers(title, Plain, query), Is.True);

    /// <summary>A dish goes by two names here, and a site may print either one.</summary>
    [TestCase("Milk tart", "melktert")]
    [TestCase("Traditional milk tart", "melktert")]
    [TestCase("Melktert", "milk tart")]
    [TestCase("Chicken pie", "hoenderpastei")]
    [TestCase("Hoenderpastei", "chicken pie")]
    public void TheDishsOtherName_IsAnAnswer(string title, string query) =>
        Assert.That(QueryWords.Answers(title, Plain, query), Is.True);

    /// <summary>The other name counts whole: a tart that isn't milk tart is no melktert.</summary>
    [TestCase("Apple tart", "melktert")]
    [TestCase("Chicken curry", "hoenderpastei")]
    [TestCase("Pork pie", "hoenderpastei")]
    public void HalfOfTheOtherName_IsNoAnswer(string title, string query) =>
        Assert.That(QueryWords.Answers(title, Plain, query), Is.False);

    /// <summary>The address counts too: <c>Milk tart</c> at <c>/melktert/</c> is a melktert.</summary>
    [Test]
    public void AWordInTheAddress_Counts() =>
        Assert.That(QueryWords.Answers("Milk tart", new Uri("https://example.com/recipes/melktert/"), "melktert"), Is.True);

    /// <summary>The site's own name in the address doesn't count, or every page of Jan Braai would answer "braai".</summary>
    [Test]
    public void TheSitesOwnName_DoesntCount() =>
        Assert.That(QueryWords.Answers("Puttanesca", new Uri("https://braai.com/puttanesca/"), "braai chicken"), Is.False);

    /// <summary>A search of only joining words can't tell, so it keeps the hit.</summary>
    [Test]
    public void ASearchWithNoRealWord_KeepsTheHit() =>
        Assert.That(QueryWords.Answers("Puttanesca", Plain, "the and"), Is.True);
}
