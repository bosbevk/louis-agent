using LouisAgent.Diagrams.Cards;
using LouisAgent.Diagrams.Cards.Sizes;
using LouisAgent.Diagrams.Icons;

namespace LouisAgent.Diagrams.Tests.Cards.Sizes;

/// <summary>
/// How tall a card of an overview is in the new look (F134.9), and how its words are broken: a highlighted card and
/// a heading are headlines, a plain card is glass, and a card no link touches is quiet and lower.
/// </summary>
public class CardSizesTests
{
    /// <summary>The longest text a part has in the docs' pictures today, 116 characters.</summary>
    private const string LongText =
        "The sidebar, the phone's top bar and the content column, with a pack's screens in it, and what a site " +
        "adds there.";

    private static Card Card(CardLook look, string title = "The chat's frame", string? code = null,
        string text = "The whole chat.") => new("part", DiagramIcon.Message, title, code, text, look);

    /// <summary>
    /// A highlighted card is a headline: its badge, its title, its code name in a pill and its text, one under the
    /// other. It is 170 tall with one line of text, and 19 more for each further line.
    /// </summary>
    [Test]
    public void AHighlightedCard_IsAHeadline_AsTallAsItsWordsNeed()
    {
        var oneLine = CardSizes.Of(Card(CardLook.Highlight, code: "louis-agent.web"), 896, quiet: false);
        var noCode = CardSizes.Of(Card(CardLook.Highlight), 896, quiet: false);
        var longer = CardSizes.Of(Card(CardLook.Highlight, code: "louis-agent.web", text: LongText + " " + LongText),
            896, quiet: false);

        Assert.Multiple(() =>
        {
            Assert.That(oneLine.Height, Is.EqualTo(170));
            Assert.That(noCode.Height, Is.EqualTo(170 - CardSizes.PillRoom), "no pill, no room for one");
            Assert.That(longer.Height, Is.EqualTo(170 + CardSizes.HeadlineLine * (longer.TextLines.Count - 1)));
            Assert.That(longer.TextLines, Has.Count.GreaterThan(1));
        });
    }

    /// <summary>A heading is a quieter headline, and lower: 92 with one line of text.</summary>
    [Test]
    public void AHeading_IsALowerHeadline() =>
        Assert.That(CardSizes.Of(Card(CardLook.Heading), 896, quiet: false).Height, Is.EqualTo(92));

    /// <summary>A headline is never quiet, whatever links the picture has.</summary>
    [TestCase(CardLook.Highlight)]
    [TestCase(CardLook.Heading)]
    public void AHeadline_IsNeverQuiet(CardLook look) =>
        Assert.That(CardSizes.Of(Card(look), 896, quiet: true).Quiet, Is.False);

    /// <summary>
    /// A plain card is glass: its title on one line beside its badge, and three lines of text make it 132 tall,
    /// as the design draws it.
    /// </summary>
    [Test]
    public void APlainCard_HasItsTitleBesideItsBadge()
    {
        var size = CardSizes.Of(Card(CardLook.Plain,
            text: "The whole chat: its sidebar, messages, message box and search, a turn's life included."), 284,
            quiet: false);

        Assert.Multiple(() =>
        {
            Assert.That(size.TitleBelow, Is.False);
            Assert.That(size.TitleLines, Is.EqualTo(new[] { "The chat's frame" }));
            Assert.That(size.TextLines, Has.Count.EqualTo(3));
            Assert.That(size.Height, Is.EqualTo(132));
            Assert.That(size.Quiet, Is.False);
        });
    }

    /// <summary>
    /// A title too long for the room beside the badge goes under it, broken over lines that fit the card, and the
    /// card is a line taller for each.
    /// </summary>
    [Test]
    public void ALongTitleOnANarrowCard_GoesUnderItsBadge()
    {
        var beside = CardSizes.Of(Card(CardLook.Plain, title: "Short"), 207, quiet: false);
        var under = CardSizes.Of(Card(CardLook.Plain, title: "The same layers 3 to 5, in two areas"), 207,
            quiet: false);

        Assert.Multiple(() =>
        {
            Assert.That(under.TitleBelow, Is.True);
            Assert.That(under.TitleLines, Has.Count.GreaterThan(1));
            Assert.That(under.TitleLines.Max(line => line.Length) * CardSizes.TitleChar,
                Is.LessThanOrEqualTo(207 - 2 * CardSizes.Pad));
            Assert.That(under.Height, Is.EqualTo(beside.Height + CardSizes.TitleLine * under.TitleLines.Count));
        });
    }

    /// <summary>A plain card's code name is a pill under its title, and the card is taller by its room.</summary>
    [Test]
    public void ACodeName_AddsItsPillsRoom()
    {
        var bare = CardSizes.Of(Card(CardLook.Plain), 284, quiet: false);
        var named = CardSizes.Of(Card(CardLook.Plain, code: "projects/"), 284, quiet: false);

        Assert.That(named.Height, Is.EqualTo(bare.Height + CardSizes.CodeRoom));
    }

    /// <summary>
    /// A quiet card has no badge, so it is lower than glass: 74 with one line of text, and 17 more a line.
    /// </summary>
    [Test]
    public void AQuietCard_IsLower()
    {
        var one = CardSizes.Of(Card(CardLook.Plain), 437, quiet: true);
        var longer = CardSizes.Of(Card(CardLook.Plain, text: LongText), 437, quiet: true);

        Assert.Multiple(() =>
        {
            Assert.That(one.Quiet, Is.True);
            Assert.That(one.Height, Is.EqualTo(74));
            Assert.That(longer.Height, Is.EqualTo(74 + CardSizes.QuietLine * (longer.TextLines.Count - 1)));
            Assert.That(one.Height, Is.LessThan(CardSizes.Of(Card(CardLook.Plain), 437, quiet: false).Height));
        });
    }

    /// <summary>
    /// Every line of text fits its card by the measure: its length times a character's width is inside the card's
    /// sides, at each width a row of one, two, three or four cards gives.
    /// </summary>
    [TestCase(896)]
    [TestCase(437)]
    [TestCase(284)]
    [TestCase(207.5)]
    public void EveryLineOfText_FitsItsCard_ByTheMeasure(double width)
    {
        var glass = CardSizes.Of(Card(CardLook.Plain, text: LongText), width, quiet: false);
        var quiet = CardSizes.Of(Card(CardLook.Plain, text: LongText), width, quiet: true);
        var headline = CardSizes.Of(Card(CardLook.Highlight, text: LongText), width, quiet: false);

        Assert.Multiple(() =>
        {
            Assert.That(glass.TextLines.Max(line => line.Length) * CardSizes.TextChar,
                Is.LessThanOrEqualTo(width - 2 * CardSizes.Pad), "glass");
            Assert.That(quiet.TextLines.Max(line => line.Length) * CardSizes.TextChar,
                Is.LessThanOrEqualTo(width - 2 * CardSizes.QuietPad), "quiet");
            Assert.That(headline.TextLines.Max(line => line.Length) * CardSizes.HeadlineChar,
                Is.LessThanOrEqualTo(width - 2 * CardSizes.Pad), "a headline");
            Assert.That(string.Join(" ", glass.TextLines), Is.EqualTo(LongText), "no word is lost");
        });
    }
}
