using LouisAgent.Choices;

namespace LouisAgent.Api.Evals.Runs.Kinds;

/// <summary>
/// How a grader decides: by a fixed rule, by a model, or by a person. Read once where the protocol line comes in.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum EvalGraderKind
{
    // PascalCase on purpose (Louis, 2026-10-27): stored runs and the protocol carry the text as written today.
    // One lower-case style waits for protocol v2 (7c), which starts stored runs fresh.
    [WireName("FixedRule")] FixedRule,
    [WireName("Model")] Model,
    [WireName("Human")] Human,
}
