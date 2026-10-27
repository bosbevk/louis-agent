using LouisAgent.Puls.Admin.Charts;

namespace LouisAgent.Puls.Tests.Admin.Charts;

// F18 step 6a.3b (Louis: "Move logic out and shrink it", then "Move the token charts out"): the explorer's chart maths and colours leave the page for plain classes. The palette is the
// dataviz reference's six series colours in their validated order, in one place for the trend chart and the explorer's charts; a ring's arcs are a circle's stroke-dasharray, lengths out
// of a circumference of 100, each starting where the one before ended, from twelve o'clock. Written first: none of these classes existed (they were private to RunExplorer and TrendChart).
public class ChartsTests
{
    [Test]
    public void TheSixSeriesColours_AreInOnePlace_AndSeriesWrapsRoundAfterTheSixth()
    {
        Assert.That(ChartPalette.Colours, Has.Count.EqualTo(6));
        Assert.That(ChartPalette.Colours.Distinct().Count(), Is.EqualTo(6));
        Assert.That(ChartPalette.Series(0), Is.EqualTo("#3987e5"));
        Assert.That(ChartPalette.Series(5), Is.EqualTo(ChartPalette.Colours[5]));
        Assert.That(ChartPalette.Series(6), Is.EqualTo(ChartPalette.Series(0)));
        Assert.That(ChartPalette.Series(7), Is.EqualTo(ChartPalette.Series(1)));
    }

    [Test]
    public void ArcsOfOneAndThree_AreAQuarterAndThreeQuarters_EachStartingWhereTheLastEnded()
    {
        var arcs = RingArcs.Of([1, 3]);

        Assert.That(arcs.Select(a => (a.Index, a.Length, a.Offset)), Is.EqualTo(new[] { (0, 25.0, 25.0), (1, 75.0, 0.0) }));
    }

    [Test]
    public void AllZeros_GiveNoArcs_AndAZeroPartIsSkippedButKeepsItsIndex()
    {
        Assert.That(RingArcs.Of([0, 0, 0]), Is.Empty);
        Assert.That(RingArcs.Of([]), Is.Empty);
        Assert.That(RingArcs.Of([0, 2, 2]).Select(a => (a.Index, a.Length)), Is.EqualTo(new[] { (1, 50.0), (2, 50.0) }));
    }

    [Test]
    public void TheFourTokenKinds_HaveTheirNamesInOrder_AndTheirColoursFromThePalette()
    {
        Assert.That(TokenKinds.Names, Is.EqualTo(new[] { "input", "cache written", "cache read", "output" }));
        Assert.That(TokenKinds.Colours, Is.EqualTo(new[] { ChartPalette.Series(0), ChartPalette.Series(1), ChartPalette.Series(2), ChartPalette.Series(3) }));
        Assert.That(TokenKinds.Titles([12, 0, 3, 4]), Is.EqualTo(new[] { "input 12", "cache written 0", "cache read 3", "output 4" }));
    }

    [Test]
    public void ThePercent_IsRoundedToOneDecimal_AndZeroAgainstNothing()
    {
        Assert.That(ChartNumbers.Percent(1L, 3L), Is.EqualTo("33.3"));
        Assert.That(ChartNumbers.Percent(5m, 0m), Is.EqualTo("0"));
        Assert.That(ChartNumbers.Percent(1m, 2m), Is.EqualTo("50.0"), "a decimal keeps its scale, as the page always showed it");
    }

    [Test]
    public void ANumberForTheMarkup_IsInvariant_AndRoundedToThreePlaces()
    {
        Assert.That(ChartNumbers.Format(15.915494), Is.EqualTo("15.915"));
        Assert.That(ChartNumbers.Format(25), Is.EqualTo("25"));
    }
}
