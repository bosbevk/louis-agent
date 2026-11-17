using LouisAgent.Web.Components.Widget.Bar.Caption;

namespace LouisAgent.Web.Tests.Components.Widget.Bar.Caption;

/// <summary>
/// F154: the guide's words come at a steady pace, not in the bursts the model streams them in (Louis's pick A of
/// three): about 90 letters a second, faster when far behind, finished soon after the answer is done, never more than
/// has arrived and never going back. The pace keeps part letters from frame to frame, and the caption shows the whole
/// ones.
/// </summary>
public class WordPaceTests
{
    private static readonly TimeSpan OneFrame = TimeSpan.FromSeconds(1d / 60);

    /// <summary>A second of frames at the steady pace shows about 90 letters of a long answer.</summary>
    [Test]
    public void AtTheSteadyPace_ASecondShowsAbout90Letters()
    {
        double shown = 0;

        for (int frame = 0; frame < 60; frame++) shown = WordPace.Next(shown, 200, OneFrame, done: false);

        Assert.That(WordPace.Letters(shown), Is.InRange(88, 92));
    }

    /// <summary>
    /// A thousand letters behind what has arrived, it speeds up and is no more than <see cref="WordPace.FarBehind"/>
    /// behind within about a second and a half.
    /// </summary>
    [Test]
    public void FarBehind_ItClosesTheGapWithinASecondAndAHalf()
    {
        double shown = 0;

        for (int frame = 0; frame < 95; frame++) shown = WordPace.Next(shown, 1000, OneFrame, done: false);

        Assert.That(1000 - WordPace.Letters(shown), Is.LessThanOrEqualTo(WordPace.FarBehind));
    }

    /// <summary>Once the answer is done, the last 200 letters show within about 1.3 seconds.</summary>
    [Test]
    public void OnceDone_TheRestShowsWithinASecondOrSo()
    {
        double shown = 0;

        for (int frame = 0; frame < 80; frame++) shown = WordPace.Next(shown, 200, OneFrame, done: true);

        Assert.That(WordPace.Letters(shown), Is.EqualTo(200));
    }

    /// <summary>It never shows more than has arrived, and never takes back what it showed.</summary>
    [TestCase(10, 12)]
    [TestCase(50, 50)]
    [TestCase(80, 60)]
    public void ItNeverRunsAhead_NorGoesBack(double shown, int arrived)
    {
        double next = WordPace.Next(shown, arrived, TimeSpan.FromSeconds(1), done: true);

        Assert.Multiple(() =>
        {
            Assert.That(next, Is.GreaterThanOrEqualTo(shown));
            Assert.That(next, Is.LessThanOrEqualTo(Math.Max(shown, arrived)));
        });
    }
}
