using LouisAgent.Choices;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Outcomes;
using LouisAgent.Evals.Protocol;

namespace LouisAgent.Evals.Tests.Protocol;

// F18 step 7.5.2 follow-up (Louis, 2026-10-27, finding 1 A): these enums travel as PascalCase in stored runs and the protocol, so each member's [WireName] is its own name, and `.Wire()` writes
// exactly what `ToString()` wrote. The move to one lower-case style waits for protocol v2 (7c), which starts stored runs fresh.
public class PascalCaseWireTests
{
    [Test]
    public void TheHarnessEnumsStoredRunsCarry_WriteTheirMemberName()
    {
        foreach (var m in Enum.GetValues<GradeOutcome>()) Assert.That(m.Wire(), Is.EqualTo(m.ToString()), $"GradeOutcome.{m}");
        foreach (var m in Enum.GetValues<TrialOutcome>()) Assert.That(m.Wire(), Is.EqualTo(m.ToString()), $"TrialOutcome.{m}");
        foreach (var m in Enum.GetValues<GraderKind>()) Assert.That(m.Wire(), Is.EqualTo(m.ToString()), $"GraderKind.{m}");
        foreach (var m in Enum.GetValues<ModelLevel>()) Assert.That(m.Wire(), Is.EqualTo(m.ToString()), $"ModelLevel.{m}");
    }
}
