using LouisAgent.Web.Components.Packs;

namespace LouisAgent.Packs.Recipes.Web;

/// <summary>Adds the recipes pack's own views to a site's list. A site calls this once when it starts.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0086-a-packs-screens-are-described.md">ADR 0086: a pack's screens are described</seealso>
public static class RecipeWebViews
{
    /// <summary>Registers them; anything registered after this one wins, so a demo can still take over.</summary>
    public static void AddTo(PackViews views) => views.UseScreen<RecipeLibraryView>(RecipeUsageLine.ScreenName);
}
