namespace LouisAgent.Composition.Packs.Reports;

/// <summary>A report as the page shows it: the names of its count columns, and its rows in the order they read.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values</seealso>
public sealed record ReportTable(IReadOnlyList<string> Columns, IReadOnlyList<ReportRow> Rows);
