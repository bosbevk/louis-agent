using LouisAgent.SessionStream.Pacing;

namespace LouisAgent.SessionStream.Tests.Pacing;

/// <summary>
/// F154: an answer's words come at a steady pace, not in the bursts the model streams them in (Louis's pick A of
/// three): about 90 letters a second, faster the further behind, finished soon after the answer is done, never more
/// than has arrived and never going back. F155 made the faster pace continuous, the backlog over a catch-up window,
/// so it grows with the backlog and never steps (Louis: "web should change to also continuous"). The pace keeps part
/// letters from frame to frame, and the caller shows the whole ones.
/// </summary>
public class WordPaceTests
{
    private static readonly TimeSpan OneFrame = TimeSpan.FromSeconds(1d / 60);

    /// <summary>While less than a window's worth is waiting, it keeps the steady 90 letters a second.</summary>
    [Test]
    public void WithLittleWaiting_ItKeepsTheSteady90LettersASecond()
    {
        double shown = WordPace.Next(0, 80, OneFrame, done: false);

        // A frame's length is whole ticks, so a sixtieth of a second is a hair short of it.
        Assert.That(shown, Is.EqualTo(90d / 60).Within(1e-3));
    }

    /// <summary>A thousand letters behind what has arrived, it is within 240 of them in about a second and a half.</summary>
    [Test]
    public void FarBehind_ItClosesTheGapWithinASecondAndAHalf()
    {
        double shown = 0;

        for (int frame = 0; frame < 95; frame++) shown = WordPace.Next(shown, 1000, OneFrame, done: false);

        Assert.That(1000 - WordPace.Letters(shown), Is.LessThanOrEqualTo(240));
    }

    /// <summary>
    /// F155: the pace grows with the backlog and has no step: a frame at 239 letters behind and one at 241 show all but
    /// the same, where a threshold at 240 had them jump from the steady pace to the catch-up one.
    /// </summary>
    [Test]
    public void ThePace_GrowsWithTheBacklog_WithNoStep()
    {
        double at239 = WordPace.Next(0, 239, OneFrame, done: false);
        double at241 = WordPace.Next(0, 241, OneFrame, done: false);

        Assert.That(at241 - at239, Is.LessThan(0.1));
    }

    /// <summary>F155: 240 letters waiting all show within about two seconds, where the step took about 2.7.</summary>
    [Test]
    public void TwoHundredFortyWaiting_ShowWithinAboutTwoSeconds()
    {
        double shown = 0;

        for (int frame = 0; frame < 125; frame++) shown = WordPace.Next(shown, 240, OneFrame, done: false);

        Assert.That(WordPace.Letters(shown), Is.EqualTo(240));
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
