using LouisAgent.Packs.Calculator.Tools;

namespace LouisAgent.Packs.Calculator.Tests.Tools;

/// <summary>The tool the model calls: many sums in one call, each answered on its own line.</summary>
public class CalculatorToolsTests
{
    private readonly CalculatorTools _tools = new();

    /// <summary>Each sum comes back beside its result, so the model can see which is which.</summary>
    [Test]
    public void EachSum_ComesBackWithItsResult() =>
        Assert.That(_tools.Calculate(["500*3/2", "1*2/3"]), Is.EqualTo("500*3/2 = 750\n1*2/3 = 0.6667"));

    /// <summary>One bad sum says why, and doesn't cost the others their results.</summary>
    [Test]
    public void ABadSum_SaysWhy_AndTheOthersStillAnswer()
    {
        string answer = _tools.Calculate(["1/0", "2+2"]);

        Assert.That(answer, Does.Contain("1/0: can't be worked out"));
        Assert.That(answer, Does.Contain("2+2 = 4"));
    }

    /// <summary>A call with nothing to work out says what to send.</summary>
    [Test]
    public void NoSums_SaysWhatToSend() =>
        Assert.That(_tools.Calculate([]), Does.Contain("needs at least one sum"));

    /// <summary>The answer stays bounded, as a tool's output must.</summary>
    [Test]
    public void TooManySums_IsRefused() =>
        Assert.That(_tools.Calculate([.. Enumerable.Repeat("1+1", CalculatorTools.MostSums + 1)]),
            Does.Contain($"At most {CalculatorTools.MostSums} sums"));
}
