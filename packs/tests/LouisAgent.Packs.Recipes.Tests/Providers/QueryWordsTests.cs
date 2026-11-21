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
    [TestCase("Ushatini", "pap en sous")]
    [TestCase("Sausages in tomato and onion gravy", "pap and sous")]
    [TestCase("Sheba relish", "pap sous")]
    public void TheDishsOtherName_IsAnAnswer(string title, string query) =>
        Assert.That(QueryWords.Answers(title, Plain, query), Is.True);

    /// <summary>A search for pap en sous can hear what else to try, whichever way it's spelled.</summary>
    [TestCase("pap en sous")]
    [TestCase("pap and sous")]
    [TestCase("Pap Sous")]
    public void PapEnSous_HasOtherNamesToTry(string query) =>
        Assert.That(QueryWords.AlsoCalled(query), Does.Contain("pap and sheba").And.Contain("ushatini"));

    /// <summary>Other dishes have nothing to add, so their results don't grow.</summary>
    [TestCase("bobotie")]
    [TestCase("melktert")]
    [TestCase("stywe pap")]
    public void OtherDishes_HaveNoOtherNamesToTry(string query) =>
        Assert.That(QueryWords.AlsoCalled(query), Is.Empty);

    /// <summary>A search that found nothing hears the dish's other name, in the other language or spelling.</summary>
    [TestCase("appeltert", "apple tart")]
    [TestCase("apple tart", "appeltert")]
    [TestCase("ingelegde vis", "pickled fish")]
    [TestCase("karringmelkbeskuit", "buttermilk rusks")]
    [TestCase("pampoenkoekies", "pumpkin fritters")]
    [TestCase("oxtail stew", "beesstert")]
    [TestCase("sjokoladekoek", "chocolate cake")]
    [TestCase("samoosas", "samosas")]
    [TestCase("macaroni and cheese", "mac and cheese")]
    [TestCase("melktert", "milk tart")]
    public void ASearchThatFoundNothing_HearsTheOtherName(string query, string other) =>
        Assert.That(QueryWords.OtherNames(query), Does.Contain(other));

    /// <summary>A dish with one name has nothing to say, and pap en sous only the names worth another search.</summary>
    [Test]
    public void OtherNames_AreOnlyTheOnesWorthASearch()
    {
        Assert.That(QueryWords.OtherNames("bobotie"), Is.Empty);
        Assert.That(QueryWords.OtherNames("pap en sous"), Is.EqualTo(QueryWords.AlsoCalled("pap en sous")));
    }

    /// <summary>A page printing the dish in the other language answers the search, both ways round.</summary>
    [TestCase("Apple tart", "appeltert")]
    [TestCase("Appeltert", "apple tart")]
    [TestCase("Pickled fish", "ingelegde vis")]
    [TestCase("Beesstert in rooiwyn", "oxtail")]
    public void ThePageInTheOtherLanguage_IsAnAnswer(string title, string query) =>
        Assert.That(QueryWords.Answers(title, Plain, query), Is.True);

    /// <summary>The other name counts whole: a tart that isn't milk tart is no melktert.</summary>
    [TestCase("Apple tart", "melktert")]
    [TestCase("Chicken curry", "hoenderpastei")]
    [TestCase("Pork pie", "hoenderpastei")]
    public void HalfOfTheOtherName_IsNoAnswer(string title, string query) =>
        Assert.That(QueryWords.Answers(title, Plain, query), Is.False);

    /// <summary>A hit holds the whole name of the dish, a plural or a longer form counting.</summary>
    [TestCase("Traditional Pap en Sous", "pap en sous")]
    [TestCase("Pap and sous", "pap en sous")]
    [TestCase("Creamy chicken and mushroom pot pies", "chicken pie")]
    [TestCase("Boerewors Roll", "boerewors rolls")]
    [TestCase("Boerewors Sausage Rolls", "boerewors rolls")]
    public void TheWholeNameOfTheDish_IsAnAnswer(string title, string query) =>
        Assert.That(QueryWords.Answers(title, Plain, query), Is.True);

    /// <summary>One word of the dish's name isn't the dish: these are what the search used to offer for it.</summary>
    [TestCase("A simple Stywe Pap recipe", "pap en sous")]
    [TestCase("Krummel Pap Recipe", "pap en sous")]
    [TestCase("Bone-in rib-eye steak sous vide with a pepper sauce", "pap en sous")]
    [TestCase("Romerige macaroni-en-kaas", "pap en sous")]
    [TestCase("Boerewors Braai Pie", "boerewors rolls")]
    [TestCase("Chicken curry", "chicken pie")]
    public void OneWordOfTheName_IsNoAnswer(string title, string query) =>
        Assert.That(QueryWords.Answers(title, Plain, query), Is.False);

    /// <summary>A longer search can miss one word, and a word that only praises the dish doesn't count.</summary>
    [TestCase("Orange Chicken Marinade", "braai chicken marinade")]
    [TestCase("Chicken pie", "easy chicken pie")]
    [TestCase("Bobotie", "lekker tradisionele bobotie")]
    public void ALongerSearch_CanMissAWord(string title, string query) =>
        Assert.That(QueryWords.Answers(title, Plain, query), Is.True);

    /// <summary>The address counts too: <c>Milk tart</c> at <c>/melktert/</c> is a melktert.</summary>
    [Test]
    public void AWordInTheAddress_Counts() =>
        Assert.That(QueryWords.Answers("Milk tart", new Uri("https://example.com/recipes/melktert/"), "melktert"), Is.True);

    /// <summary>The site's name in the address doesn't count, or all of Jan Braai would answer "braai".</summary>
    [Test]
    public void TheSitesOwnName_DoesntCount() =>
        Assert.That(QueryWords.Answers("Puttanesca", new Uri("https://braai.com/puttanesca/"), "braai chicken"), Is.False);

    /// <summary>A search of only joining words can't tell, so it keeps the hit.</summary>
    [Test]
    public void ASearchWithNoRealWord_KeepsTheHit() =>
        Assert.That(QueryWords.Answers("Puttanesca", Plain, "the and"), Is.True);
}
