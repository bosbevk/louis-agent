using LouisAgent.Diagrams.Icons;
using LouisAgent.Web.Components.Site.Addresses;
using LouisAgent.Web.Components.Site.Showroom;
using LouisAgent.Web.Components.Site.Showroom.Start;
using LouisAgent.Web.Components.Site.Tiles;
using LouisAgent.Web.Components.Widget;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Site.Showroom.Start;

/// <summary>
/// F134.4: when the guide shows a demo's address on the start, that demo's window comes forward. The guide's call
/// names a place on the page, the widget's script opens that place as a visitor's press would (a card is pressed),
/// and the dock's button for the demo is that card.
/// </summary>
public class ShowroomStartGuideTests
{
    private static readonly ShowroomDemos Demos = new([
        new ShowroomDemo("/bakery-demo", "A bakery", "Bread at dawn", DemoGroup.ForYourBusiness, TileAccent.Green,
            DiagramIcon.Bed),
        new ShowroomDemo("/florist-demo", "A florist", "Flowers by noon", DemoGroup.ForYourBusiness, TileAccent.Blue,
            DiagramIcon.Dining),
    ]);

    private static readonly SiteAddresses Site = new("home", ["/demos"]);

    /// <summary>
    /// The guide shows the florist's address. The place it names is a card on the start, and pressing it, as the
    /// widget's script does, brings the florist forward from behind the bakery.
    /// </summary>
    [Test]
    public void TheGuideShowingADemosAddress_BringsThatDemoForward()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(Demos);
        ctx.Services.AddSingleton(Site);
        ctx.Services.GetRequiredService<NavigationManager>().NavigateTo("/demos");
        var start = ctx.Render<ShowroomStart>(parameters => parameters
            .Add(shown => shown.Featured, "/bakery-demo")
            .Add(shown => shown.Title, "Watch the agents at work."));

        var target = GuidePlaces.Target("/demos/florist-demo", Site);
        start.Find($"[data-card]#{target?.Place}").Click();

        Assert.Multiple(() =>
        {
            Assert.That(target?.Page, Is.EqualTo("/demos"), "the start's own page, so the browser stays");
            Assert.That(start.Find(".deck-item.is-forward .window-address").TextContent, Does.Contain("florist"));
        });
    }
}
