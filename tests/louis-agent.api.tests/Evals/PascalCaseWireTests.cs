using LouisAgent.Api.Evals.Reviews;
using LouisAgent.Choices;

namespace LouisAgent.Api.Tests.Evals;

/// <summary>
/// The store keeps a person's verdict in PascalCase, exactly as it's named in the code; the portal's API words it in
/// lower case, spelled out on its own and not taken from the stored name. The protocol's kinds are the contract's now
/// and travel in its kebab-case words (F18 step 7c.3).
/// </summary>
public class PascalCaseWireTests
{
    /// <summary>A verdict the store keeps is written the way its member is named.</summary>
    [Test]
    public void TheVerdictTheStoreKeeps_WritesItsMemberName()
    {
        foreach (var m in Enum.GetValues<HumanVerdict>())
            Assert.That(m.Wire(), Is.EqualTo(m.ToString()), $"HumanVerdict.{m}");
    }

    /// <summary>The API says pass, fail or unsure in lower case, whatever the store writes.</summary>
    [Test]
    public void TheApiWordsAVerdictInLowerCase_NotFromItsWireName() =>
        Assert.That(Enum.GetValues<HumanVerdict>().Select(HumanVerdicts.Word),
            Is.EqualTo(new[] { "pass", "fail", "unsure" }));
}
