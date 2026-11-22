using LouisAgent.Evals.Files;

namespace LouisAgent.Evals.Tests.Files;

/// <summary>
/// A fact's words become a fact (F126.2 step 4): each of the seven is read by its first word, whatever its case, and
/// writes itself back as the file has it; words that are no fact, or that a kind can't take, are refused with why.
/// </summary>
public class FactTests
{
    /// <summary>Each of the seven reads into its kind and its argument.</summary>
    [TestCase("no-error", FactKind.NoError, "")]
    [TestCase("used ReadDoc", FactKind.Used, "ReadDoc")]
    [TestCase("not-used ShowPage", FactKind.NotUsed, "ShowPage")]
    [TestCase("under 30s", FactKind.Under, "30")]
    [TestCase("never-says the admin key", FactKind.NeverSays, "the admin key")]
    [TestCase("routed-to seemeeu-guesthouse", FactKind.RoutedTo, "seemeeu-guesthouse")]
    [TestCase("continued seemeeu-guesthouse", FactKind.Continued, "seemeeu-guesthouse")]
    public void EachFact_IsRead(string words, FactKind kind, string argument) =>
        Assert.That(Fact.Parse(words), Is.EqualTo(new Fact(kind, argument)));

    /// <summary>The first word is read whatever its case.</summary>
    [Test]
    public void TheFirstWord_IsReadWhateverItsCase() =>
        Assert.That(Fact.Parse("USED ReadDoc"), Is.EqualTo(new Fact(FactKind.Used, "ReadDoc")));

    /// <summary>A fact writes itself back as the file has it.</summary>
    [TestCase("no-error")]
    [TestCase("used ReadDoc")]
    [TestCase("under 30s")]
    [TestCase("never-says the admin key")]
    [TestCase("continued seemeeu-guesthouse")]
    public void AFact_WritesItselfBack(string words) => Assert.That(Fact.Parse(words).ToString(), Is.EqualTo(words));

    /// <summary>A first word that is no fact is refused, naming the seven, read from the enum.</summary>
    [TestCase("notused ReadDoc")]
    [TestCase("similar the answer")]
    [TestCase("")]
    public void AFirstWordThatIsNoFact_IsRefused_NamingTheSeven(string words) =>
        Assert.That(() => Fact.Parse(words), Throws.InstanceOf<FormatException>()
            .With.Message.Contains("no-error, used, not-used, under, never-says, routed-to, continued"));

    /// <summary>Each kind refuses what it can't take, and says what it takes.</summary>
    [TestCase("no-error now", "takes nothing")]
    [TestCase("used", "names one tool")]
    [TestCase("used Read Doc", "names one tool")]
    [TestCase("under 30", "seconds")]
    [TestCase("under 0s", "seconds")]
    [TestCase("under fast", "seconds")]
    [TestCase("routed-to Not An Address", "address")]
    [TestCase("continued", "address")]
    [TestCase("never-says", "names the text")]
    public void WordsAKindCantTake_AreRefused_WithTheReason(string words, string reason) =>
        Assert.That(() => Fact.Parse(words), Throws.InstanceOf<FormatException>().With.Message.Contains(reason));
}
