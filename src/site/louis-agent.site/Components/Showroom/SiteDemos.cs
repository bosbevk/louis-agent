using LouisAgent.Diagrams.Icons;
using LouisAgent.Site.Components.Showroom.Windows;
using LouisAgent.Web.Components.Site.Showroom;
using LouisAgent.Web.Components.Site.Tiles;

namespace LouisAgent.Site.Components.Showroom;

/// <summary>
/// The site's demos in its showroom (F127.1), in the rail's order: the three made-up businesses, the guesthouse leading
/// (Louis: "make the guesthouse the main one"), then the agents at work. Each has its own colour everywhere it shows
/// (Louis's pick 1a: purple, the fifth accent, is the triage team's). Each business names the window that shows it
/// on the start, and the word in that window's bar, its made-up website's address. The agents at work name none:
/// the start shows the businesses only.
/// </summary>
public static class SiteDemos
{
    /// <summary>The five, registered once in <c>Program.cs</c> for the showroom to draw.</summary>
    public static ShowroomDemos All { get; } = new([
        new ShowroomDemo("/guesthouse-demo", "A guesthouse", "A weekend by the beach, with the dog",
            DemoGroup.ForYourBusiness, TileAccent.Blue, DiagramIcon.Bed, typeof(SeemeeuWindow), "seemeeu.example"),
        new ShowroomDemo("/restaurant-demo", "A restaurant", "Sunday lunch at ten at night", DemoGroup.ForYourBusiness,
            TileAccent.Green, DiagramIcon.Dining, typeof(WheatfieldWindow), "wheatfieldkitchen.example"),
        new ShowroomDemo("/law-practice-demo", "A law practice", "No advice, a consultation", DemoGroup.ForYourBusiness,
            TileAccent.Yellow, DiagramIcon.Scales, typeof(KoringlandWindow), "koringland.example"),
        new ShowroomDemo("/coding-agent", "The coding agent", "Fixes a real bug", DemoGroup.AgentsAtWork,
            TileAccent.Red, DiagramIcon.Code),
        new ShowroomDemo("/triage-demo", "The triage team", "Sorts and fixes an error", DemoGroup.AgentsAtWork,
            TileAccent.Purple, DiagramIcon.Exchange),
    ]);
}
