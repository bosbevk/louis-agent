using System.Net;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Reports;
using LouisAgent.Web.Api;
using LouisAgent.Web.Storage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Pages.Admin.Data;

/// <summary>
/// The Reports page (F35.7): every report the instance's packs keep, as the contract's <c>ReportsReply</c>, each in its
/// own card. It knows no pack, so a new report shows here with no change.
/// </summary>
/// <remarks>
/// ADR 0042: the page shows what it last fetched, and Reload fetches again; it keeps no count of its own.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0042-the-web-shows-the-live-results.md">ADR 0042: the web shows the live results</seealso>
public partial class Reports
{
    private IReadOnlyList<ReportReply>? _reports;
    private string? _error;
    private bool _needsKey, _loading;

    [Inject] private AgentApi Api { get; set; } = null!;
    [Inject] private ChatStore Store { get; set; } = null!;

    /// <summary>Loads the admin key, then the reports.</summary>
    protected override async Task OnInitializedAsync()
    {
        if (!Store.Loaded) await Store.LoadAsync();
        await LoadAsync();
    }

    /// <summary>Reads the reports; a refused key shows the box to enter one.</summary>
    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;
        try
        {
            var reply = await Api.GetAsync<ReportsReply>("v1/admin/reports", AdminJson.SnakeCase);
            _reports = reply.Reports;
        }
        catch (AgentApiException ex)
        {
            _needsKey = ex.Status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden;
            _error = ex.Message;
        }
        catch (HttpRequestException ex)
        {
            _error = ex.Message;
        }

        _loading = false;
    }
}
