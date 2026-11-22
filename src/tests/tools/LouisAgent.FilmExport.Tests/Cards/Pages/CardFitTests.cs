using LouisAgent.FilmExport.Cards.Pages;

namespace LouisAgent.FilmExport.Tests.Cards.Pages;

/// <summary>
/// A card is written only when it is right (F169.6): its words end inside the middle a cut to 3 to 2 keeps, and its
/// type loaded, since a card drawn while Geist was missing looks nearly right and is wrong.
/// </summary>
public class CardFitTests
{
    /// <summary>Words that end at the middle's edge or before, in the right type, may be written.</summary>
    [TestCase(903)]
    [TestCase(1072)]
    public void WordsInsideTheMiddle_MayBeWritten(double endsAt) =>
        Assert.That(CardFit.Check("/getting-started", endsAt, typeLoaded: true), Is.Null);

    /// <summary>Words that end past it may not, and the message names the page, where they end and the limit.</summary>
    [Test]
    public void WordsPastTheMiddle_AreRefused()
    {
        string? refusal = CardFit.Check("/getting-started", 1090.4, typeLoaded: true);

        Assert.That(refusal, Does.Contain("/getting-started").And.Contain("1090").And.Contain("1072"));
    }

    /// <summary>A card whose type didn't load may not be written, whatever its words.</summary>
    [Test]
    public void ACardWithoutItsType_IsRefused() =>
        Assert.That(CardFit.Check("/features", 640, typeLoaded: false), Does.Contain("/features"));
}
