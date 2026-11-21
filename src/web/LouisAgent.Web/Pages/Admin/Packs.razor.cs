using System.Net;
using LouisAgent.Web.Api;
using LouisAgent.Web.Components.Packs;
using LouisAgent.Web.Components.Screens;
using LouisAgent.Web.Storage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Pages.Admin;

/// <summary>
/// The packs area's page, the same on every site that names the packs (F144): each screen the packs offer, with what
/// it shows, and the uploads they take, each the way to its own page.
/// </summary>
/// <remarks>
/// It reads what the sidebar's menu reads (<see cref="PackOffer"/>), so the two never disagree. The page shows what it
/// last fetched, and Reload fetches again (ADR 0042); a refused key asks for it, as every admin page does.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0042-the-web-shows-the-live-results.md">ADR 0042: the web shows the live results</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0086-a-packs-screens-are-described.md">ADR 0086: a pack's screens are described</seealso>
public partial class Packs
{
    private PackOffer? _offer;
    private string? _error;
    private bool _needsKey, _loading;

    [Inject] private AgentApi Api { get; set; } = null!;
    [Inject] private ChatStore Store { get; set; } = null!;
    [Inject] private PackViews Views { get; set; } = null!;

    /// <summary>Loads the admin key, then what the packs offer.</summary>
    protected override async Task OnInitializedAsync()
    {
        if (!Store.Loaded) await Store.LoadAsync();
        await LoadAsync();
    }

    /// <summary>Reads what the packs offer; a refused key shows the box to enter one.</summary>
    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;
        try
        {
            _offer = await PackOffer.ReadAsync(Api, Views);
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
