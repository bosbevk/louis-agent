namespace LouisAgent.Composition.Packs.Reports;

/// <summary>One row of a report: what it's about ("All days", "2026-11-06"), and a count per column.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values</seealso>
public sealed record ReportRow(string Label, IReadOnlyList<long> Counts);
