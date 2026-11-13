using System.Net;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Overview;
using LouisAgent.Web.Api;
using LouisAgent.Web.Components.Areas.Overview;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Pages.Admin;

/// <summary>
/// The admin's overview, the same on every site (F127.8): what its agents did today and this month, by part of the
/// site and by who answered, and the latest talks, with the tools the site counts under its own names.
/// </summary>
/// <remarks>
/// The page shows what it last fetched, and Reload fetches again (ADR 0042); a refused key asks for it, as every admin
/// page does. The costs come as the ledger charged them and are shown in the reader's currency.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0042-the-web-shows-the-live-results.md">ADR 0042: the web shows the live results</seealso>
public partial class Overview
{
    private OverviewReply? _reply;
    private string? _error;
    private bool _needsKey, _loading;

    [Inject] private AgentApi Api { get; set; } = null!;
    [Inject] private ChatStore Store { get; set; } = null!;
    [Inject] private CurrencyState Currency { get; set; } = null!;
    [Inject] private OverviewCounts Counts { get; set; } = null!;

    /// <summary>Loads the admin key and the reader's currency, then the overview.</summary>
    protected override async Task OnInitializedAsync()
    {
        if (!Store.Loaded) await Store.LoadAsync();
        // The costs show in the reader's currency, as the chat's do; the admin has no widget to load it.
        await Currency.LoadAsync();
        await LoadAsync();
    }

    /// <summary>Reads today's overview; a refused key shows the box to enter one.</summary>
    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;
        try
        {
            _reply = await Api.GetAsync<OverviewReply>("v1/admin/overview", AdminJson.SnakeCase);
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
