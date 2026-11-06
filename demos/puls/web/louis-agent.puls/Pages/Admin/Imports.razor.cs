using System.Net;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Imports;
using LouisAgent.Puls.Admin.Imports;
using LouisAgent.Web.Api;
using LouisAgent.Web.Storage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Pages.Admin;

/// <summary>
/// The Uploads page (F55): every data upload the instance's packs take, as the contract's <c>ImportsReply</c>, each
/// in its own card.
/// </summary>
/// <remarks>
/// ADR 0042: the page shows what it last fetched, and Reload fetches again, starting each upload afresh.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0042-the-web-shows-the-live-results.md">ADR 0042: the web shows the live results</seealso>
public partial class Imports
{
    private IReadOnlyList<ImportForm>? _forms;
    private string? _error;
    private bool _needsKey, _loading;

    [Inject] private AgentApi Api { get; set; } = null!;
    [Inject] private ChatStore Store { get; set; } = null!;

    /// <summary>Loads the admin key, then the list.</summary>
    protected override async Task OnInitializedAsync()
    {
        if (!Store.Loaded) await Store.LoadAsync();
        await LoadAsync();
    }

    /// <summary>Reads the uploads; a refused key shows the box to enter one.</summary>
    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;
        try
        {
            var reply = await Api.GetAsync<ImportsReply>("v1/admin/imports", AdminJson.SnakeCase);
            _forms = reply.Imports.Select(upload => new ImportForm(upload)).ToList();
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
