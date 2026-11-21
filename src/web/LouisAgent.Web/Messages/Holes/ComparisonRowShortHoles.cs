namespace LouisAgent.Web.Messages.Holes;

/// <summary>The holes of a refused comparison row: its topic, how many cells it has and how many columns there are.</summary>
public sealed record ComparisonRowShortHoles(string Topic, int Cells, int Columns);
