namespace LouisAgent.Evals.Grading;

/// <summary>What a grader judges with: a fixed rule (free and repeatable), a model (calibrated first), or a person (Louis's labels).</summary>
public enum GraderKind
{
    FixedRule,
    Model,
    Human,
}
