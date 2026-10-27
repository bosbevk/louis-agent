using LouisAgent.Choices;
namespace LouisAgent.Evals.Grading;

/// <summary>What a grader judges with: a fixed rule (free and repeatable), a model (calibrated first), or a person (Louis's labels).</summary>
public enum GraderKind
{
    // PascalCase on purpose (Louis, 2026-10-27): stored runs and the protocol carry the text as written today; one lower-case style waits for protocol v2 (7c), which starts stored runs fresh.
    [WireName("FixedRule")] FixedRule,
    [WireName("Model")] Model,
    [WireName("Human")] Human,
}
