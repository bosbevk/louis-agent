using LouisAgent.Web.Api;
using LouisAgent.Web.Components.Packs;
using LouisAgent.Web.Storage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Screens;

/// <summary>
/// The packs area's links in the admin's sidebar (F144): the packs page first, then a link for each screen the packs
/// offer, so a new screen appears with no change to any site. Without the key it stays empty and says nothing: the
/// page you're on already says what's wrong, and asks for the key.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0086-a-packs-screens-are-described.md">ADR 0086: a pack's screens are described</seealso>
public partial class PackMenu : IDisposable
{
    private PackOffer _offer = PackOffer.None;

    [Inject] private AgentApi Api { get; set; } = null!;
    [Inject] private ChatStore Store { get; set; } = null!;
    [Inject] private PackViews Views { get; set; } = null!;

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

    /// <summary>Reads what the packs offer; if that fails the menu is empty, as the page says why.</summary>
    private async Task ReadAsync()
    {
        try
        {
            _offer = await PackOffer.ReadAsync(Api, Views);
        }
        catch (AgentApiException)
        {
            _offer = PackOffer.None;
        }
        catch (HttpRequestException)
        {
            _offer = PackOffer.None;
        }
    }
}
