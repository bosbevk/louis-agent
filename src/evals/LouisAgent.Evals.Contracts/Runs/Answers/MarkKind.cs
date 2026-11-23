namespace LouisAgent.Evals.Contracts.Runs.Answers;

/// <summary>What a mark was: a fact code told, or a criterion the judge marked.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum MarkKind
{
    /// <summary>A fact, told by code from the transcript.</summary>
    Fact,

    /// <summary>A criterion, marked by the judge with its reason.</summary>
    Criterion
}
