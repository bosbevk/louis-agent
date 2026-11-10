using System.Net;
using System.Reflection;
using LouisAgent.Site.Components.Showroom;
using LouisAgent.Site.Layout;
using LouisAgent.Site.Pages.Agents.Showroom;
using LouisAgent.Web.Components.Recorded;
using LouisAgent.Web.Components.Site.Showroom;
using LouisAgent.Web.Components.Site.Tiles;
using LouisAgent.Web.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Site.Tests.Showroom;

/// <summary>
/// F127.1: the demos live in the showroom. The site registers its five, each in its group and colour, and a business's
/// demo plays on a mock of its own website, with its sample brand and the cues of what to watch.
/// </summary>
public class ShowroomPagesTests
{
    /// <summary>
    /// The site's five demos, in the rail's order, each in its group and its own colour (Louis's pick 1a).
    /// </summary>
    [Test]
    public void TheSite_RegistersItsFiveDemos_EachInItsColour()
    {
        var demos = SiteDemos.All.All.Select(d => (d.Address, d.Group, d.Accent));

        Assert.That(demos,
            Is.EqualTo(new[]
            {
                ("/restaurant-demo", DemoGroup.ForYourBusiness, TileAccent.Green),
                ("/law-practice-demo", DemoGroup.ForYourBusiness, TileAccent.Blue),
                ("/guesthouse-demo", DemoGroup.ForYourBusiness, TileAccent.Yellow),
                ("/coding-agent", DemoGroup.AgentsAtWork, TileAccent.Red),
                ("/triage-demo", DemoGroup.AgentsAtWork, TileAccent.Purple),
            }));
    }

    /// <summary>A business's demo page draws in the showroom, not the landing's frame.</summary>
    [TestCase(typeof(RestaurantDemoPage))]
    [TestCase(typeof(LawPracticeDemoPage))]
    [TestCase(typeof(GuesthouseDemoPage))]
    public void ABusinessDemo_DrawsInTheShowroom(Type page) =>
        Assert.That(page.GetCustomAttribute<LayoutAttribute>()?.LayoutType, Is.EqualTo(typeof(ShowroomLayout)));

    /// <summary>
    /// A business's demo plays on a mock of its own website, in its colour, with its own address, its sample brand's
    /// mark, and three cues of what to watch beside it.
    /// </summary>
    [TestCase(typeof(RestaurantDemoPage), "accent-green", "wheatfieldkitchen.example")]
    [TestCase(typeof(LawPracticeDemoPage), "accent-blue", "koringland.example")]
    [TestCase(typeof(GuesthouseDemoPage), "accent-yellow", "seemeeu.example")]
    public void ABusinessDemo_PlaysOnItsOwnWebsite(Type page, string accent, string address)
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(new ChatText());
        ctx.Services.AddSingleton(new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)))
        {
            BaseAddress = new Uri("http://site.test/"),
        });

        var cut = ctx.Render(builder =>
        {
            builder.OpenComponent(0, page);
            builder.CloseComponent();
        });

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find(".demo-stage").ClassList, Does.Contain(accent));
            Assert.That(cut.Find(".site-mock .mock-address").TextContent, Is.EqualTo(address));
            Assert.That(cut.Find(".site-mock .mock-header .mock-mark svg"), Is.Not.Null, "its sample brand's mark");
            Assert.That(cut.Find(".site-mock .mock-tagline").TextContent, Is.Not.Empty, "its tagline");
            Assert.That(cut.FindAll(".stage-cues .cues-list > li"), Has.Count.EqualTo(3));
            Assert.That(cut.FindAll("h1"), Has.Count.EqualTo(1));
            var chat = cut.FindComponent<RecordedChat>().Instance;
            Assert.That((chat.Pace, chat.TurnGap), Is.EqualTo((2.5, 2200)),
                "at a person's pace (Louis: \"slow it down like realtime\"), a breath before each question");
        });
    }
}
