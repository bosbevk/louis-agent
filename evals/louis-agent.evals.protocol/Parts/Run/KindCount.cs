using LouisAgent.Evals.Protocol.Kinds;

namespace LouisAgent.Evals.Protocol.Parts.Run;

/// <summary>How the graders of one kind (rules, models, people) graded a slice of a mode, added up.</summary>
public sealed record KindCount(GraderKind Kind, int Passed, int Failed, int Unknown);
