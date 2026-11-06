namespace LouisAgent.Api.Contracts.Admin.Reports;

/// <summary>The numbers the packs keep about themselves (F35.7): <c>GET /v1/admin/reports</c>.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public sealed record ReportsReply(IReadOnlyList<ReportReply> Reports);

/// <summary>One report: its name, what its numbers are, its count columns' names, and its rows in order.</summary>
public sealed record ReportReply(
    string Name,
    string Description,
    IReadOnlyList<string> Columns,
    IReadOnlyList<ReportRowReply> Rows);

/// <summary>One row of a report: what it's about, and a count per column.</summary>
public sealed record ReportRowReply(string Label, IReadOnlyList<long> Counts);
