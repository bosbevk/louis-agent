using System.Globalization;
using LouisAgent.Api.Contracts;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Overview;
using LouisAgent.Api.Endpoints.Routing;
using LouisAgent.Api.Messages;
using LouisAgent.Api.Messages.Holes.Admin;
using LouisAgent.Api.Sessions.Log;
using LouisAgent.Composition.Messages;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Pricing.Rates;
using LouisAgent.Core.Usage.Records;
using LouisAgent.Core.Usage.Reports;
using Microsoft.AspNetCore.Http.HttpResults;
using OverviewAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.Admin.Overview.OverviewReply>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;

namespace LouisAgent.Api.Endpoints.Admin.Overview;

/// <summary>
/// The admin's overview of what a site's agents did (F127.8): the day asked for, today by default, read from the
/// month's turn log and ledger in the host's log folder, costs in the currency the ledger charged them in.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
internal static class OverviewHandlers
{
    /// <summary>Adds the overview's route to the admin group, behind its key.</summary>
    public static void MapOverview(this RouteGroupBuilder admin) => admin.MapGet("/overview", GetOverview);

    /// <summary>The overview of the day; a 400 for a day that isn't one.</summary>
    public static OverviewAnswer GetOverview(string? day, AgentEngineFactory engines, AgentEngine engine)
    {
        if (DayOf(day) is not { } asked)
        {
            var unknown = new UnknownDayHoles(day!);
            return ApiErrors.BadRequest(MessageText.Format(ApiUserText.UnknownDay, unknown));
        }

        // The month's two files, beside each other in the log folder; none when the host keeps no ledger.
        var options = engines.Context.Options;
        string? folder = options.UsageLedger ? options.LogDirectory : null;
        var turns = new TurnLog(folder).Read(asked).ToList();
        // In the currency the ledger charged: the admin's page converts for its reader, as every cost the web shows is.
        var charged = DisplayCurrency.Charged(engine.Display?.Base ?? "USD");
        var reply = OverviewReader.Of(asked, turns, LedgerOf(folder, asked), charged);
        return TypedResults.Json(reply, AdminJson.SnakeCase);
    }

    /// <summary>The day asked for: today when none is given, nothing when it isn't a date.</summary>
    /// <remarks>Guard clauses (ADR 0051): no day, then a day that reads.</remarks>
    private static DateOnly? DayOf(string? day)
    {
        if (string.IsNullOrWhiteSpace(day)) return DateOnly.FromDateTime(DateTime.UtcNow);
        return DateOnly.TryParseExact(day, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None,
            out var read)
            ? read
            : null;
    }

    /// <summary>The month's ledger, as the usage sink wrote it; none without a folder or a file.</summary>
    private static List<UsageRecord> LedgerOf(string? folder, DateOnly month)
    {
        if (folder is null) return [];
        string path = Path.Combine(folder, $"usage-{month:yyyy-MM}.jsonl");
        return File.Exists(path) ? UsageReport.Read(path).ToList() : [];
    }
}
