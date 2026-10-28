using LouisAgent.Choices;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Outcomes;

namespace LouisAgent.Evals.Tests.Protocol;

/// <summary>
/// The harness's grade and trial words travel as PascalCase in version 1's stored runs and lines, so each member's
/// wire name is its own name. Step 7c.2c moves them to the contract's lower-case words and this test goes.
/// </summary>
public class PascalCaseWireTests
{
    /// <summary><c>.Wire()</c> writes exactly the member's name for every enum version 1 still carries.</summary>
    [Test]
    public void TheHarnessEnumsStoredRunsCarry_WriteTheirMemberName()
    {
        foreach (var m in Enum.GetValues<GradeOutcome>())
            Assert.That(m.Wire(), Is.EqualTo(m.ToString()), $"GradeOutcome.{m}");
        foreach (var m in Enum.GetValues<TrialOutcome>())
            Assert.That(m.Wire(), Is.EqualTo(m.ToString()), $"TrialOutcome.{m}");
        foreach (var m in Enum.GetValues<GraderKind>())
            Assert.That(m.Wire(), Is.EqualTo(m.ToString()), $"GraderKind.{m}");
    }
}
