namespace LouisAgent.Diagrams.Engine.Flow.Rows;

/// <summary>
/// What a rule for the rows is told of a band before it says how tall the band's rows are: its tallest box, the
/// columns it has boxes in, and how many rows it has.
/// </summary>
/// <param name="Tallest">How tall its tallest box is.</param>
/// <param name="Columns">The columns it has boxes in. Two bands that share none stand beside each other.</param>
/// <param name="Rows">How many rows it has: as many as its fullest column has boxes.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values, classes for things</seealso>
public sealed record BandShape(double Tallest, IReadOnlySet<int> Columns, int Rows);
