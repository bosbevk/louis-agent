using LouisAgent.Web.Components.Site.Tiles;

namespace LouisAgent.Web.Components.Site.Showroom;

/// <summary>
/// One demo in a site's showroom (F127.1): where it is, what it's called and the line under its name, which group it's
/// in, the colour it wears everywhere, and the sign on its cover.
/// </summary>
/// <param name="Address">The demo's page, such as <c>/restaurant-demo</c>.</param>
/// <param name="Name">Its name in the rail, such as "A restaurant".</param>
/// <param name="Line">The few words under the name: what happens in it.</param>
/// <param name="Group">What a visitor comes to it for.</param>
/// <param name="Accent">Its colour: the cover, the stage's light, the cues and the offer.</param>
/// <param name="Sign">The sign on its cover, one or two characters.</param>
public sealed record ShowroomDemo(
    string Address,
    string Name,
    string Line,
    DemoGroup Group,
    TileAccent Accent,
    string Sign);
