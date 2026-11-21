namespace LouisAgent.Web.Components.Site.Tables;

/// <summary>One row of a comparison: its topic, and its cells, one under each of the comparison's columns.</summary>
public sealed record ComparisonRow(string Topic, IReadOnlyList<string> Cells);
