using LouisAgent.Site.Layout;
using LouisAgent.Site.Pages.Agents;
using LouisAgent.Site.Pages.Agents.Showroom;
using LouisAgent.Site.Tests.Pages;
using LouisAgent.Web.Components.Recorded;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Text;
using LouisAgent.Web.Usage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Site.Tests.Pages.Agents;

/// <summary>
/// The showroom's demos (F128.2): a restaurant, a law practice and a guesthouse (since F127.1, in place of the doctor's
/// practice), each on its own page, its
/// conversation recorded once and played as its customer would see it, said to be made up, then the offer; first on
/// the demos list and the menu.
/// </summary>
public class ShowroomPagesTests
{
    /// <summary>A page, rendered in a context whose site serves nothing.</summary>
    private static IRenderedComponent<IComponent> Render(Type page)
    {
        var ctx = SitePageContext.Create();
        return ctx.Render(builder =>
        {
            builder.OpenComponent(0, page);
            builder.CloseComponent();
        });
    }

    /// <summary>Each page is at its address, says its business is made up, and plays its conversation as a customer
    /// sees it.</summary>
    [TestCase(typeof(RestaurantDemoPage), "/restaurant-demo", "Wheatfield Kitchen")]
    [TestCase(typeof(LawPracticeDemoPage), "/law-practice-demo", "Koringland Attorneys")]
    [TestCase(typeof(GuesthouseDemoPage), "/guesthouse-demo", "Seemeeu Guesthouse")]
    public void APage_PlaysItsBusinesssConversation(Type page, string address, string business)
    {
        var routes = page.GetCustomAttributes(typeof(RouteAttribute), false).Cast<RouteAttribute>();

        var cut = Render(page);
        var chat = cut.FindComponent<RecordedChat>().Instance;

        Assert.Multiple(() =>
        {
            Assert.That(routes.Select(route => route.Template), Is.EqualTo(new[] { address }));
            Assert.That(cut.Markup, Does.Contain(business).And.Contain("made-up business"));
            Assert.That(chat.ShowWork, Is.False, "as its customer would see it");
            Assert.That(chat.Turns, Has.Count.GreaterThanOrEqualTo(3), "a conversation, not one question");
            Assert.That(chat.Turns.Select(turn => turn.Source), Is.All.StartsWith("recordings/showroom/"));
            Assert.That(cut.FindAll("a[href='/contact']"), Is.Not.Empty);
        });
    }

    /// <summary>Each conversation's questions are word for word what the recording script asks.</summary>
    [TestCase(typeof(RestaurantDemoPage))]
    [TestCase(typeof(LawPracticeDemoPage))]
    [TestCase(typeof(GuesthouseDemoPage))]
    public void TheQuestions_AreTheScripts(Type page)
    {
        string script = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "src/site/recordings/record-showroom.sh"));

        var chat = Render(page).FindComponent<RecordedChat>().Instance;

        Assert.That(chat.Turns.Where(turn => turn.Question.Contains('\'') || !script.Contains(turn.Question)),
            Is.Empty, "word for word, and no apostrophe, so the script quotes it plainly");
    }

    /// <summary>The three come first on the demos list.</summary>
    [Test]
    public void TheDemos_ListTheBusinessesFirst()
    {
        var links = Render(typeof(DemosPage)).FindAll("article.panel a").Select(a => a.GetAttribute("href")).ToList();

        Assert.That(links.Take(3),
            Is.EqualTo(new[] { "/restaurant-demo", "/law-practice-demo", "/guesthouse-demo" }));
    }

    /// <summary>The menu's demos list them first too.</summary>
    [Test]
    public void TheMenu_ListsTheBusinessesFirst()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var settings = new ChatSettings();
        var api = FixtureApi.Client(settings);
        ctx.Services.AddSingleton(settings);
        ctx.Services.AddSingleton(api);
        ctx.Services.AddSingleton(new CurrencyState(api, ctx.JSInterop.JSRuntime));
        ctx.Services.AddSingleton(new ChatText());

        var demos = ctx.Render<SiteLayout>().FindAll(".site-nav .site-menu")
            .Single(menu => menu.QuerySelector(".site-menu-label")!.TextContent == "Demos");

        Assert.That(demos.QuerySelectorAll("a").Select(a => a.GetAttribute("href")),
            Is.EqualTo(new[]
            {
                "/demos", "/restaurant-demo", "/law-practice-demo", "/guesthouse-demo", "/coding-agent",
                "/triage-demo"
            }));
    }
}
