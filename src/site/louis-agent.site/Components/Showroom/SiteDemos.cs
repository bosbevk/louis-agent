using LouisAgent.Diagrams.Icons;
using LouisAgent.Site.Components.Showroom.Windows;
using LouisAgent.Web.Components.Site.Showroom;
using LouisAgent.Web.Components.Site.Tiles;

namespace LouisAgent.Site.Components.Showroom;

/// <summary>
/// The site's demos in its showroom (F127.1), in the rail's order: the three made-up businesses, the guesthouse leading
/// (Louis: "make the guesthouse the main one"). Each has its own colour everywhere it shows, and names the window that
/// shows it on the start, and the word in that window's bar, its made-up website's address.
/// </summary>
public static class SiteDemos
{
    /// <summary>The three, registered once in <c>Program.cs</c> for the showroom to draw.</summary>
    public static ShowroomDemos All { get; } = new([
        new ShowroomDemo("/guesthouse-demo", "A guesthouse", "A weekend by the beach, with the dog",
            DemoGroup.ForYourBusiness, TileAccent.Blue, DiagramIcon.Bed, typeof(SeemeeuWindow), "seemeeu.example"),
        new ShowroomDemo("/restaurant-demo", "A restaurant", "Sunday lunch at ten at night", DemoGroup.ForYourBusiness,
            TileAccent.Green, DiagramIcon.Dining, typeof(WheatfieldWindow), "wheatfieldkitchen.example"),
        new ShowroomDemo("/law-practice-demo", "A law practice", "No advice, a consultation", DemoGroup.ForYourBusiness,
            TileAccent.Yellow, DiagramIcon.Scales, typeof(KoringlandWindow), "koringland.example"),    ]);
}
