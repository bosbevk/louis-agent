namespace LouisAgent.Puls.Admin.Explorer.Legends;

/// <summary>One entry of a chart's legend: its colour's dot and what it stands for.</summary>
public sealed record LegendEntry(string Colour, string Label);
