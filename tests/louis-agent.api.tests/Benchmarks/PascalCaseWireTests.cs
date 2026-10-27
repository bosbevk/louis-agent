using LouisAgent.Api.Benchmarks.Reviews;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Choices;

namespace LouisAgent.Api.Tests.Benchmarks;

// F18 step 7.5.2 follow-up (Louis, 2026-10-27, finding 1 A): the stored text is PascalCase and `.Wire()` keeps it; the portal's API words a review's verdict in lower case, spelled out apart from the wire name.
public class PascalCaseWireTests
{
    [Test]
    public void TheEnumsTheStoreKeeps_WriteTheirMemberName()
    {
        foreach (var m in Enum.GetValues<BenchmarkOutcome>())
            Assert.That(m.Wire(), Is.EqualTo(m.ToString()), $"BenchmarkOutcome.{m}");
        foreach (var m in Enum.GetValues<BenchmarkGraderKind>())
            Assert.That(m.Wire(), Is.EqualTo(m.ToString()), $"BenchmarkGraderKind.{m}");
        foreach (var m in Enum.GetValues<BenchmarkModelLevel>())
            Assert.That(m.Wire(), Is.EqualTo(m.ToString()), $"BenchmarkModelLevel.{m}");
        foreach (var m in Enum.GetValues<HumanVerdict>())
            Assert.That(m.Wire(), Is.EqualTo(m.ToString()), $"HumanVerdict.{m}");
    }

    [Test]
    public void TheApiWordsAVerdictInLowerCase_NotFromItsWireName() =>
        Assert.That(Enum.GetValues<HumanVerdict>().Select(HumanVerdicts.Word),
            Is.EqualTo(new[] { "pass", "fail", "unsure" }));
}
