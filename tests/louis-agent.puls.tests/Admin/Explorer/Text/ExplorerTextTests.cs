using LouisAgent.Puls.Admin.Explorer.Text;

namespace LouisAgent.Puls.Tests.Admin.Explorer.Text;

/// <summary>How the explorer writes money, seconds, counts and a question cut to fit.</summary>
public class ExplorerTextTests
{
    /// <summary>A known cost is money; an unknown one is a dash, never a zero.</summary>
    [Test]
    public void Money_IsADashWhenUnknown()
    {
        Assert.That(ExplorerText.Money(0.012m, null), Is.EqualTo("$0.012"));
        Assert.That(ExplorerText.Money(null, null), Is.EqualTo("—"));
    }

    /// <summary>Seconds with one decimal, the same on every machine.</summary>
    [Test]
    public void Seconds_HaveOneDecimal() => Assert.That(ExplorerText.Secs(6.25), Is.EqualTo("6.3"));

    /// <summary>A count with its thousands, or a dash when unknown.</summary>
    [Test]
    public void ACount_IsADashWhenUnknown()
    {
        Assert.That(ExplorerText.Count(1200), Is.EqualTo("1,200"));
        Assert.That(ExplorerText.Count(null), Is.EqualTo("—"));
    }

    /// <summary>Nothing stays nothing, a short question stays whole, a long one is cut with an ellipsis.</summary>
    [Test]
    public void ABlurb_CutsALongQuestion()
    {
        Assert.That(ExplorerText.Blurb(null), Is.EqualTo(""));
        Assert.That(ExplorerText.Blurb(new string('a', 56)), Is.EqualTo(new string('a', 56)));
        Assert.That(ExplorerText.Blurb(new string('a', 54) + " bc"), Is.EqualTo(new string('a', 54) + "…"));
    }
}
