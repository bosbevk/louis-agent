using LouisAgent.Packs.Files.Web.Views;
using LouisAgent.Web.Components.Packs;

namespace LouisAgent.Packs.Files.Web;

/// <summary>Adds the files pack's own view to a site's list. A site calls this once when it starts.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0086-a-packs-screens-are-described.md">ADR 0086: a pack's screens are described</seealso>
public static class FilesWebViews
{
    /// <summary>The screen's name, which the pack gave it; this project can't reference the pack.</summary>
    private const string ScreenName = "workspace-files";

    /// <summary>Registers it; anything registered after this one wins, so a demo can still take over.</summary>
    public static void AddTo(PackViews views) => views.UseScreen<FilesView>(ScreenName);
}
