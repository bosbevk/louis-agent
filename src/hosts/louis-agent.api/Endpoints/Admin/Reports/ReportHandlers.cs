using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Reports;
using LouisAgent.Composition.Packs.Reports;
using LouisAgent.Core.Hosting;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LouisAgent.Api.Endpoints.Admin.Reports;

/// <summary>
/// The numbers the packs keep about themselves (F35.7), read when the page opens. The API knows no pack: it asks the
/// catalogue for the reports and hands back each one's table (ADR 0027).
/// </summary>
/// <remarks>A named endpoint returning <c>TypedResults</c> (ADR 0045, 0051).</remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
internal static class ReportHandlers
{
    /// <summary>Every report the packs list, each with its table, in the order they list them.</summary>
    public static async Task<JsonHttpResult<ReportsReply>> GetReports(AgentEngineFactory engines,
        CancellationToken cancellationToken)
    {
        // Each reads its own store, so a slow one doesn't hold up the others; the replies keep the catalogue's order.
        var replies = await Task.WhenAll(engines.Catalog.Reports.Select(report => ReplyOf(report, cancellationToken)));
        return TypedResults.Json(new ReportsReply(replies), AdminJson.SnakeCase);
    }

    /// <summary>One report as the page reads it.</summary>
    private static async Task<ReportReply> ReplyOf(IDataReport report, CancellationToken cancellationToken)
    {
        var table = await report.ReadAsync(cancellationToken);
        return new ReportReply(report.Name, report.Description, table.Columns,
            [.. table.Rows.Select(row => new ReportRowReply(row.Label, row.Counts))]);
    }
}
