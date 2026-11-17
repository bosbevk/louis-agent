using LouisAgent.Web.Chats;
using LouisAgent.Web.Components.Widget.Bar.Caption.Motion;

namespace LouisAgent.Web.Tests.Components.Widget.Bar.Caption.Motion;

/// <summary>
/// F154: an answer the bar stops showing, put away or followed by the next, does not vanish: it leaves, fading, and
/// only goes once it says it has (Louis's pick A of three).
/// </summary>
public class CaptionHandOverTests
{
    /// <summary>An answer put away begins to leave.</summary>
    [Test]
    public void AnAnswerPutAway_BeginsToLeave()
    {
        var first = new Turn();
        var handOver = new CaptionHandOver();

        handOver.Showing(first);
        handOver.Showing(null);

        Assert.That(handOver.Leaving, Is.EqualTo(new[] { first }));
    }

    /// <summary>The next answer coming sends the last one on its way, while it comes in.</summary>
    [Test]
    public void TheNextAnswer_SendsTheLastOnItsWay()
    {
        var first = new Turn();
        var next = new Turn();
        var handOver = new CaptionHandOver();

        handOver.Showing(first);
        handOver.Showing(next);

        Assert.That(handOver.Leaving, Is.EqualTo(new[] { first }));
    }

    /// <summary>Shown again and again, an answer stays; once it says it has left, it is gone.</summary>
    [Test]
    public void AnAnswerShownAgain_Stays_AndOnceLeft_IsGone()
    {
        var first = new Turn();
        var handOver = new CaptionHandOver();

        handOver.Showing(first);
        handOver.Showing(first);
        bool stayed = handOver.Leaving.Count == 0;
        handOver.Showing(null);
        handOver.Left(first);

        Assert.Multiple(() =>
        {
            Assert.That(stayed, Is.True);
            Assert.That(handOver.Leaving, Is.Empty);
        });
    }
}
