using LouisAgent.Site.Resumes;

namespace LouisAgent.Site.Tests.Resumes;

/// <summary>
/// F142: the CV's dates as JSON Resume writes them (a year, or a year and month) shown the way a reader expects, and a
/// job that hasn't ended running to the word the page gives for now.
/// </summary>
public class ResumeDatesTests
{
    /// <summary>A year and month reads as the month's short name and the year; a year alone stays a year.</summary>
    [TestCase("2021-03", "Mar 2021")]
    [TestCase("2010-12", "Dec 2010")]
    [TestCase("2004", "2004")]
    public void ADate_ReadsAsAReaderWritesIt(string date, string shown) =>
        Assert.That(ResumeDates.Of(date), Is.EqualTo(shown));

    /// <summary>A span reads from its start to its end, or to "now" when it hasn't ended.</summary>
    [TestCase("2021-03", "2024-06", "Mar 2021 – Jun 2024")]
    [TestCase("2022-03", null, "Mar 2022 – now")]
    [TestCase("2005", "2006", "2005 – 2006")]
    public void ASpan_RunsFromItsStartToItsEnd(string start, string? end, string shown) =>
        Assert.That(ResumeDates.Span(start, end, "now"), Is.EqualTo(shown));

    /// <summary>A span that starts and ends in the same month, or has only an end, is that one date.</summary>
    [TestCase("2010-04", "2010-04", "Apr 2010")]
    [TestCase(null, "2004", "2004")]
    public void ASpanOfOneDate_IsThatDate(string? start, string end, string shown) =>
        Assert.That(ResumeDates.Span(start, end, "now"), Is.EqualTo(shown));
}
