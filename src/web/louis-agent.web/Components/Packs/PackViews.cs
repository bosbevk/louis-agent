using LouisAgent.Web.Components.Screens.Table;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Packs;

/// <summary>Says which component draws a screen: the plain table, unless someone registered their own.</summary>
/// <remarks>
/// Registry (Fowler): a site fills it once at start and everything else only reads it, so a pack's view is found by
/// name and the generic page references no pack. The last to register wins, which gives the order of precedence by
/// itself: the framework's default, then a pack's view, then a demo's, as <c>Program.cs</c> lists them.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0086-a-packs-screens-are-described.md">ADR 0086: a pack's screens are described</seealso>
/// <seealso href="https://martinfowler.com/eaaCatalog/registry.html">Registry (Fowler)</seealso>
public sealed class PackViews
{
    private readonly Dictionary<string, Type> _screens = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>This component draws that screen from here on.</summary>
    public PackViews UseScreen<TView>(string screenName) where TView : ComponentBase, IScreenView
    {
        _screens[screenName] = typeof(TView);
        return this;
    }

    /// <summary>The component that draws this screen.</summary>
    public Type ScreenView(string screenName) => _screens.GetValueOrDefault(screenName, typeof(ScreenTable));
}
