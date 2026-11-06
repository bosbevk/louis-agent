using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Screens;
using LouisAgent.Web.Api;
using LouisAgent.Web.Components.Packs;
using LouisAgent.Web.Storage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Screens;

/// <summary>
/// The sidebar's link for each screen the packs offer, so a new screen appears with no change to any site. Without the
/// key it stays empty and says nothing: the page you're on already says what's wrong, and asks for the key.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0086-a-packs-screens-are-described.md">ADR 0086: a pack's screens are described</seealso>
public partial class PackMenu : IDisposable
{
    private IReadOnlyList<ScreenReply> _screens = [];

    [Inject] private AgentApi Api { get; set; } = null!;
    [Inject] private ChatStore Store { get; set; } = null!;
    [Inject] private PackViews Views { get; set; } = null!;

    /// <summary>The address of the page that draws this screen.</summary>
    private static string AddressOf(ScreenReply screen) => $"admin/screens/{Uri.EscapeDataString(screen.Name)}";

    /// <summary>Loads the admin key the first time and reads the list, then again whenever a key is saved.</summary>
    protected override async Task OnInitializedAsync()
    {
        Store.AdminKeySaved += OnKeySaved;
        if (!Store.Loaded) await Store.LoadAsync();
        await ReadAsync();
    }

    /// <summary>Stops listening, since the store outlives this menu.</summary>
    public void Dispose()
    {
        Store.AdminKeySaved -= OnKeySaved;
        GC.SuppressFinalize(this);
    }

    /// <summary>A new key may open what the last one couldn't.</summary>
    private void OnKeySaved() => _ = InvokeAsync(async () =>
    {
        await ReadAsync();
        StateHasChanged();
    });

    /// <summary>Reads the list; if that fails the menu is empty, as the page says why.</summary>
    private async Task ReadAsync()
    {
        try
        {
            var reply = await Api.GetAsync<ScreensReply>("v1/admin/screens", AdminJson.SnakeCase);
            _screens = [.. reply.Screens.Where(Views.CanDraw)];
        }
        catch (AgentApiException)
        {
            _screens = [];
        }
        catch (HttpRequestException)
        {
            _screens = [];
        }
    }
}
