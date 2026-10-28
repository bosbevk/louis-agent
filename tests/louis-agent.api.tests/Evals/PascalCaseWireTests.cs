using LouisAgent.Api.Evals.Reviews;
using LouisAgent.Api.Evals.Runs.Kinds;
using LouisAgent.Choices;

namespace LouisAgent.Api.Tests.Evals;

/// <summary>
/// The store keeps these choices in PascalCase, exactly as they're named in the code. The portal's API words a
/// review's verdict in lower case, spelled out on its own and not taken from the stored name.
/// </summary>
public class PascalCaseWireTests
{
    /// <summary>Every choice the store keeps is written the way its member is named.</summary>
    [Test]
    public void TheEnumsTheStoreKeeps_WriteTheirMemberName()
    {
        foreach (var m in Enum.GetValues<EvalOutcome>())
            Assert.That(m.Wire(), Is.EqualTo(m.ToString()), $"BenchmarkOutcome.{m}");
        foreach (var m in Enum.GetValues<EvalGraderKind>())
            Assert.That(m.Wire(), Is.EqualTo(m.ToString()), $"BenchmarkGraderKind.{m}");
        foreach (var m in Enum.GetValues<EvalModelLevel>())
            Assert.That(m.Wire(), Is.EqualTo(m.ToString()), $"BenchmarkModelLevel.{m}");
        foreach (var m in Enum.GetValues<HumanVerdict>())
            Assert.That(m.Wire(), Is.EqualTo(m.ToString()), $"HumanVerdict.{m}");
    }

    /// <summary>The API says pass, fail or unsure in lower case, whatever the store writes.</summary>
    [Test]
    public void TheApiWordsAVerdictInLowerCase_NotFromItsWireName() =>
        Assert.That(Enum.GetValues<HumanVerdict>().Select(HumanVerdicts.Word),
            Is.EqualTo(new[] { "pass", "fail", "unsure" }));
}
