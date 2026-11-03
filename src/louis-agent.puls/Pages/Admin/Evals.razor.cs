using System.Net;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Evals;
using LouisAgent.Puls.Admin.Evals.List;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Storage;
using LouisAgent.Web.Shared.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Pages.Admin;

/// <summary>
/// The Evals page: every eval the runner has, what it is, and its runs. An eval that never ran is still there, and
/// says so. It reads the list as the contract's <c>EvalListReply</c>.
/// </summary>
/// <remarks>
/// ADR 0042: the page shows what it last fetched, and Reload fetches again; it keeps no copy of its own.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0042-the-web-shows-the-live-results.md">ADR 0042: the web shows the live results</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public partial class Evals : IDisposable
{
    private EvalListReply? _data;
    private string? _error;
    private bool _needsKey, _loading;

    [Inject] private AgentApi Api { get; set; } = null!;
    [Inject] private ChatStore Store { get; set; } = null!;
    [Inject] private CurrencyState Currency { get; set; } = null!;

    /// <summary>The eval whose details are open (`?eval=`); none opens the eval that ran last.</summary>
    [SupplyParameterFromQuery(Name = "eval")]
    public string? Eval { get; set; }

    /// <summary>
    /// The eval the details pane shows: the one in the address when the list has it, else the default
    /// one.
    /// </summary>
    private EvalRow? Chosen => _data is { } data
        ? data.Evals.FirstOrDefault(e => e.Name == Eval) ??
          data.Evals.FirstOrDefault(e => e.Name == EvalListView.DefaultEval(data))
        : null;

    /// <summary>Loads the admin key and the currency, then the list.</summary>
    protected override async Task OnInitializedAsync()
    {
        if (!Store.Loaded) await Store.LoadAsync();
        // Costs in the browser's currency (F14), as everywhere in Puls; the ledger's are USD.
        Currency.Changed += StateHasChanged;
        await Currency.LoadAsync();
        await LoadAsync();
    }

    /// <summary>Stops listening for the currency.</summary>
    public void Dispose() => Currency.Changed -= StateHasChanged;

    /// <summary>Reads the evals and their runs; a refused key shows the box to enter one.</summary>
    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;
        try
        {
            _data = await Api.GetAsync<EvalListReply>("v1/admin/evals", AdminJson.SnakeCase);
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
