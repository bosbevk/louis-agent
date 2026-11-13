using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Imports;
using LouisAgent.Api.Contracts.Admin.Screens;
using LouisAgent.Web.Api;
using LouisAgent.Web.Components.Packs;

namespace LouisAgent.Web.Components.Screens;

/// <summary>
/// What the instance's packs offer the admin (F144): the screens this site can draw and the uploads they take. The
/// sidebar's menu, the packs page and the packs' card on the home all read it, so they never disagree.
/// </summary>
/// <param name="Screens">The screens this site can draw, in the packs' order.</param>
/// <param name="Uploads">The uploads the packs take.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0086-a-packs-screens-are-described.md">ADR 0086: a pack's screens are described</seealso>
public sealed record PackOffer(IReadOnlyList<ScreenReply> Screens, IReadOnlyList<ImportRow> Uploads)
{
    /// <summary>Nothing offered, or nothing read.</summary>
    public static readonly PackOffer None = new([], []);

    /// <summary>Whether the packs offer nothing at all.</summary>
    public bool IsEmpty => Screens.Count == 0 && Uploads.Count == 0;

    /// <summary>The address of the page that draws this screen.</summary>
    public static string AddressOf(ScreenReply screen) => $"admin/screens/{Uri.EscapeDataString(screen.Name)}";

    /// <summary>
    /// Reads the screens and the uploads; a screen only a pack's own view can draw is left out where the site lacks
    /// the view. A refused key or a lost API throws, for the reader to say so its own way.
    /// </summary>
    public static async Task<PackOffer> ReadAsync(AgentApi api, PackViews views)
    {
        var screens = await api.GetAsync<ScreensReply>("v1/admin/screens", AdminJson.SnakeCase);
        var uploads = await api.GetAsync<ImportsReply>("v1/admin/imports", AdminJson.SnakeCase);
        return new PackOffer([.. screens.Screens.Where(views.CanDraw)], uploads.Imports);
    }
}
