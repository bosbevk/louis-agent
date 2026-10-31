using LouisAgent.Packs.Shops.Checks.Reading;

namespace LouisAgent.Packs.Shops.Tests.Checks.Reading;

/// <summary>
/// The two lines a shop rule looks for in an answer: that its prices are estimates, and that the shop has no branch in
/// the town. Pinned on their own (F49.3 step 2), so the plain scans that replace the patterns (ADR 0057) read every
/// case as the patterns did.
/// </summary>
public class ShopLinesTests
{
    /// <summary>The estimates line, in English or Afrikaans, in any case and anywhere in the answer.</summary>
    [TestCase("*Prices are estimates, not checked at a shop.*")]
    [TestCase("Die lys.\n*Pryse is skattings, nie by 'n winkel nagegaan nie.*")]
    [TestCase("PRICES ARE ESTIMATES")]
    public void TheEstimatesLine_IsFound(string answer) => Assert.That(ShopLines.EstimatesLine(answer), Is.True);

    /// <summary>Other words about estimates are no estimates line, and neither is no answer.</summary>
    [TestCase("Prices are an estimate.")]
    [TestCase("The prices estimate the basket.")]
    [TestCase("Pryse is geskat.")]
    [TestCase(null)]
    public void OtherWords_AreNoEstimatesLine(string? answer) => Assert.That(ShopLines.EstimatesLine(answer), Is.False);

    /// <summary>
    /// "No" or "geen", then "branch" or "tak", as whole words, within forty characters of one sentence or cell.
    /// </summary>
    [TestCase("There is no PnP branch in Prince Albert.")]
    [TestCase("Daar is geen PnP-tak in Prins Albert nie.")]
    [TestCase("No branch of Pick n Pay is near you.")]
    [TestCase("There is no Pick n Pay or Woolworths branch in town.")]
    [TestCase("GEEN TAK NIE")]
    public void TheNoBranchLine_IsFound(string answer) => Assert.That(ShopLines.NoBranchLine(answer), Is.True);

    /// <summary>
    /// The two words in different sentences, cells or lines, too far apart, or only as parts of other words, are no
    /// no-branch line.
    /// </summary>
    [TestCase("No. The branch in town is closed on Sundays.")]
    [TestCase("| no | branch |")]
    [TestCase("no\nbranch")]
    [TestCase("There is no reason at all, none whatsoever, to skip the branch.")]
    [TestCase("Pick n Pay has no branches here.")]
    [TestCase("Nog 'n tak in die dorp.")]
    [TestCase("Daar is geensins 'n tak.")]
    [TestCase("nobranch")]
    [TestCase(null)]
    public void OtherWords_AreNoNoBranchLine(string? answer) => Assert.That(ShopLines.NoBranchLine(answer), Is.False);
}
