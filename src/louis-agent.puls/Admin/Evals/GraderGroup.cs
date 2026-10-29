using LouisAgent.Api.Contracts.Admin.Evals;

namespace LouisAgent.Puls.Admin.Evals;

/// <summary>
/// An eval's graders that judge the same way, under the words for that way ("fixed rule", "model", "a person"), so
/// the card can show a rule apart from a judge.
/// </summary>
public sealed record GraderGroup(string Kind, IReadOnlyList<EvalGraderRow> Graders);
