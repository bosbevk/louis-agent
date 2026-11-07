using System.Net;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Screens;
using LouisAgent.Web.Api;
using LouisAgent.Web.Components.Packs;
using LouisAgent.Web.Storage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Pages.Admin;

/// <summary>
/// The one page every pack's screen opens on, so a pack ships no page of its own and a site writes none: it reads the
/// description by name and hands it to whichever component draws that screen.
/// </summary>
/// <remarks>
/// Plugin (Fowler): the page reads the component from <see cref="PackViews" /> and draws it with
/// <see cref="DynamicComponent" />, so it references no pack, and a pack's view or a demo's takes over with no change
/// here. Naming the components here would make the framework depend on every pack.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0086-a-packs-screens-are-described.md">ADR 0086: a pack's screens are described</seealso>
/// <seealso href="https://martinfowler.com/eaaCatalog/plugin.html">Plugin (Fowler)</seealso>
public partial class Screen
{
    private string? _opened, _error;
    private bool _needsKey, _loading = true;
    private ScreenReply? _screen;

    [Inject] private AgentApi Api { get; set; } = null!;
    [Inject] private ChatStore Store { get; set; } = null!;
    [Inject] private PackViews Views { get; set; } = null!;

    /// <summary>The screen's name, from the address.</summary>
    [Parameter] public string Name { get; set; } = "";

    /// <summary>What the view is handed.</summary>
    private Dictionary<string, object> ViewParameters => new() { [nameof(IScreenView.Screen)] = _screen! };

    /// <summary>Loads the admin key the first time, then reads whichever screen the address names.</summary>
    protected override async Task OnParametersSetAsync()
    {
        if (Name == _opened)
            return;

        _opened = Name;
        if (!Store.Loaded) await Store.LoadAsync();
        await LoadAsync();
    }

    /// <summary>Finds the screen in the list; a refused key shows the box to enter one.</summary>
    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;
        try
        {
            var reply = await Api.GetAsync<ScreensReply>("v1/admin/screens", AdminJson.SnakeCase);
            _screen = reply.Screens.Where(Views.CanDraw)
                .FirstOrDefault(s => s.Name.Equals(Name, StringComparison.OrdinalIgnoreCase));
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
