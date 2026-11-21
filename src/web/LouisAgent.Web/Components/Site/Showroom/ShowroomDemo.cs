using LouisAgent.Diagrams.Icons;
using LouisAgent.Web.Components.Site.Tiles;

namespace LouisAgent.Web.Components.Site.Showroom;

/// <summary>
/// One demo in a site's showroom (F127.1): where it is, what it's called and the line under its name, which group it's
/// in, the colour it wears everywhere, its icon, and what its window on the start shows.
/// </summary>
/// <remarks>
/// A slot named by its type, as a site area's admin card is: the site names the component that fills the demo's
/// window, and the framework draws it without knowing what it is. A demo that names none still has a window, with
/// a plain body. Chosen over a window that draws its own frame, which every site's window would have to repeat,
/// and over a bar that always shows the demo's own address, which can't say whose website the window is.
/// </remarks>
/// <param name="Address">The demo's page, such as <c>/restaurant-demo</c>.</param>
/// <param name="Name">Its name in the rail, such as "A restaurant".</param>
/// <param name="Line">The few words under the name: what happens in it.</param>
/// <param name="Group">What a visitor comes to it for.</param>
/// <param name="Accent">Its colour: the cover, the stage's light, the cues and the offer.</param>
/// <param name="Icon">
/// Its icon, a line drawing the framework holds (F134.1). It was an emoji, which each system drew its own way.
/// </param>
/// <param name="Window">
/// The component that fills its window on the start, such as a look at its website. With none, the window shows
/// the demo's icon over its name and its line.
/// </param>
/// <param name="WindowAddress">
/// The word in its window's bar, such as <c>seemeeu.example</c>. With none, the bar shows the demo's own address.
/// </param>
public sealed record ShowroomDemo(
    string Address,
    string Name,
    string Line,
    DemoGroup Group,
    TileAccent Accent,
    DiagramIcon Icon,
    Type? Window = null,
    string? WindowAddress = null);
