using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Template.Evals.Example.Grading;

/// <summary>
/// A grader of this eval (docs/guides/add-an-eval-set.md, section 3): one rule that reads a trial and gives a grade. Build the grade with
/// <c>Verdict</c> (Pass, Fail, Unknown), return Unknown for a turn that failed for the infrastructure (<c>Verdict.InfrastructureFailure</c>), and give
/// every reason as a message enum member (ADR 0009). A grader only this eval uses is in this folder; one two evals use is in <c>Shared/Grading/</c>.
/// </summary>
public sealed class ExampleGrader : IGrader
{
    // MCP-safe: letters, digits, '-' and '_'.
    public string Name => "example";

    // Bumped when the rule changes.
    public int Version => 1;

    // FixedRule needs no model: free and repeatable. A model grader is another kind.
    public GraderKind Kind => GraderKind.FixedRule;

    // A trial passes only if every required grader passes.
    public bool Required => true;

    // Every key this grader reads from a scenario's expected: the loader refuses a key no grader reads, and a key no scenario sets.
    public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string> { "example" };

    // The rule itself. Write it (and its tests: a pass and a fail, on made-up transcripts).
    public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}
