namespace LouisAgent.Puls.Admin.Grades;

/// <summary>One of a run's graders, as the run row sends it: its name, its version (null when the row has none, never 0) and its kind (<c>FixedRule</c>, <c>Model</c>, <c>Human</c>).</summary>
public sealed record GraderRow(string Name, int? Version, string Kind);
