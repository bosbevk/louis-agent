using LouisAgent.Web.Api;
using LouisAgent.Web.Components.Packs;
using LouisAgent.Web.Storage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Screens;

/// <summary>
/// The packs' card on the admin's home (F144): how many screens and uploads the packs offer, with the way to the packs
/// page that lists them.
/// </summary>
/// <remarks>
/// It reads what the menu and the page read (<see cref="PackOffer"/>) and shows what it last fetched (ADR 0042). When
/// the API says no, the card stays quiet and the home asks for the key, as the overview's card does.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0042-the-web-shows-the-live-results.md">ADR 0042: the web shows the live results</seealso>
public partial class PacksCard
{
    private PackOffer? _offer;
    private bool _notRead;

    [Inject] private AgentApi Api { get; set; } = null!;
    [Inject] private ChatStore Store { get; set; } = null!;
    [Inject] private PackViews Views { get; set; } = null!;

    /// <summary>Loads the admin key, then what the packs offer.</summary>
    protected override async Task OnInitializedAsync()
    {
        if (!Store.Loaded) await Store.LoadAsync();
        try
        {
            _offer = await PackOffer.ReadAsync(Api, Views);
        }
        catch (AgentApiException)
        {
            _notRead = true;
        }
        catch (HttpRequestException)
        {
            _notRead = true;
        }
    }
}
