using LouisAgent.Puls.Admin.Explorer.Tree;

namespace LouisAgent.Puls.Tests.Admin.Explorer.Tree;

/// <summary>Requests added up as the charts split them: Puls's part against the specialists', the tokens.</summary>
public class RequestSumsTests
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 27, 10, 0, 0, TimeSpan.Zero);

    /// <summary>Puls's own request and a specialist's, one with no known price.</summary>
    private static readonly RequestNode[] Rows =
    [
        new(T0, "turn", "s1", null, "m", 100, 20, 400, 50, 0.01m, 1500),
        new(T0, "turn", "s2", "s1", "m", 200, null, 0, 80, 0.03m, 2000),
        new(T0, "turn", "s2", "s1", "m", null, null, null, 5, null, 500)
    ];

    /// <summary>Each side's cost; an unknown price is left out, and a side with none priced has none.</summary>
    [Test]
    public void TheCost_IsSplitBetweenPulsAndTheSpecialist()
    {
        Assert.That(RequestSums.Cost(Rows, specialist: false), Is.EqualTo(0.01m));
        Assert.That(RequestSums.Cost(Rows, specialist: true), Is.EqualTo(0.03m));
        Assert.That(RequestSums.Cost(Rows[2..], specialist: true), Is.Null);
    }

    /// <summary>Each side's model seconds.</summary>
    [Test]
    public void TheSeconds_AreSplitBetweenPulsAndTheSpecialist()
    {
        Assert.That(RequestSums.Seconds(Rows, specialist: false), Is.EqualTo(1.5));
        Assert.That(RequestSums.Seconds(Rows, specialist: true), Is.EqualTo(2.5));
    }

    /// <summary>The four kinds of token, an unknown count as none.</summary>
    [Test]
    public void TheTokens_AreAddedUpByKind() =>
        Assert.That(RequestSums.Tokens(Rows), Is.EqualTo(new long[] { 300, 20, 400, 135 }));
}
