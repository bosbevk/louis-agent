using LouisAgent.Diagrams.Icons;
using LouisAgent.Web.Components.Site.Showroom;
using LouisAgent.Web.Components.Site.Tiles;

namespace LouisAgent.Site.Components.Showroom;

/// <summary>
/// The site's demos in its showroom (F127.1), in the rail's order: the three made-up businesses, the guesthouse leading
/// (Louis: "make the guesthouse the main one"), then the agents at work. Each has its own colour everywhere it shows
/// (Louis's pick 1a: purple, the fifth accent, is the triage team's).
/// </summary>
public static class SiteDemos
{
    /// <summary>The five, registered once in <c>Program.cs</c> for the showroom to draw.</summary>
    public static ShowroomDemos All { get; } = new([
        new ShowroomDemo("/guesthouse-demo", "A guesthouse", "A weekend by the beach, with the dog",
            DemoGroup.ForYourBusiness, TileAccent.Blue, DiagramIcon.Bed),
        new ShowroomDemo("/restaurant-demo", "A restaurant", "Sunday lunch at ten at night", DemoGroup.ForYourBusiness,
            TileAccent.Green, DiagramIcon.Dining),
        new ShowroomDemo("/law-practice-demo", "A law practice", "No advice, a consultation", DemoGroup.ForYourBusiness,
            TileAccent.Yellow, DiagramIcon.Scales),
        new ShowroomDemo("/coding-agent", "The coding agent", "Fixes a real bug", DemoGroup.AgentsAtWork,
            TileAccent.Red, DiagramIcon.Code),
        new ShowroomDemo("/triage-demo", "The triage team", "Sorts and fixes an error", DemoGroup.AgentsAtWork,
            TileAccent.Purple, DiagramIcon.Exchange),
    ]);
}
