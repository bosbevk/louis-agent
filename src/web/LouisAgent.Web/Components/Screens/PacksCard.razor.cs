using LouisAgent.Web.Components.Admin;
using LouisAgent.Web.Components.Packs;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Screens;

/// <summary>
/// The packs' card on the admin's home (F144): how many screens and uploads the packs offer, with the way to the packs
/// page that lists them.
/// </summary>
/// <remarks>
/// It reads what the menu and the page read (<see cref="PackOffer"/>) and shows what it last fetched (ADR 0042); a
/// refused key, a saved one and a lost API are <see cref="AdminCardBase"/>'s to handle.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0042-the-web-shows-the-live-results.md">ADR 0042: the web shows the live results</seealso>
public partial class PacksCard
{
    private PackOffer? _offer;

    [Inject] private PackViews Views { get; set; } = null!;

    /// <summary>What the packs offer.</summary>
    protected override async Task ReadAsync() => _offer = await PackOffer.ReadAsync(Api, Views);
}
