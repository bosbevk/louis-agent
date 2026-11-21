using LouisAgent.Packs.WhereToBuy.Branches;

namespace LouisAgent.Packs.WhereToBuy.Tests.Branches;

/// <summary>The straight-line distance between two places.</summary>
public class DistanceTests
{
    /// <summary>Malmesbury to Cape Town's centre is about 54 km; a place to itself is nothing.</summary>
    [Test]
    public void TheDistance_IsInKilometres()
    {
        Assert.That(Distance.Km(-33.4603, 18.7236, -33.9249, 18.4241), Is.EqualTo(58.6).Within(1));
        Assert.That(Distance.Km(-33.46, 18.72, -33.46, 18.72), Is.EqualTo(0));
    }
}
