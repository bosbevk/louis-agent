using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Screens;
using LouisAgent.Web.Components.Areas;
using LouisAgent.Web.Pages.Admin;
using Microsoft.AspNetCore.Components;
using LouisAgent.CodeReview.Layout;
using LouisAgent.CodeReview.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.CodeReview.Tests.Pages;

/// <summary>
/// The framework's admin pages reach the code-review app through its router, inside its admin frame, as they reach
/// every site: its controls become admin screens, so this is where they'll open; and the chat is the app's root.
/// </summary>
public class SiteRoutesTests
{
    /// <summary>Every address a page in this assembly answers to.</summary>
    private static IEnumerable<string> RoutesOf(Assembly assembly) =>
        assembly.GetTypes().SelectMany(type => type.GetCustomAttributes<RouteAttribute>())
            .Select(route => route.Template);

    /// <summary>No address is claimed twice, by the site and the framework together.</summary>
    [Test]
    public void NoAddress_IsClaimedTwice()
    {
        var routes = RoutesOf(typeof(App).Assembly).Concat(RoutesOf(typeof(Screen).Assembly)).ToList();

        var claimedTwice = routes.GroupBy(route => route, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1).Select(group => group.Key);
        Assert.That(claimedTwice, Is.Empty);
    }

    /// <summary>
    /// The chat is the root and lives at /chat too (F124: the framework's site left this app), in its own bare
    /// layout, which the router also draws by default.
    /// </summary>
    [Test]
    public void TheRoot_IsTheChat()
    {
        var chatRoutes = typeof(ChatPage).GetCustomAttributes<RouteAttribute>().Select(route => route.Template)
            .ToList();
        var chatLayout = typeof(ChatPage).GetCustomAttribute<LayoutAttribute>()?.LayoutType;
        string router = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "demos/louis-agent/web/louis-agent.code-review/App.razor"));

        Assert.Multiple(() =>
        {
            Assert.That(chatRoutes, Does.Contain("/").And.Contain("/chat"));
            Assert.That(chatLayout, Is.EqualTo(typeof(ChatLayout)), "the chat draws in its bare frame");
            Assert.That(router, Does.Contain("DefaultLayout=\"@typeof(ChatLayout)\""));
        });
    }

    /// <summary>
    /// F127.6: the app names its areas through the builder, the areas the program names too. Opening a screen's
    /// address draws the framework's page in this site's admin frame, and the eval studio, named as an area, puts
    /// its links in the admin's sidebar.
    /// </summary>
    [Test]
    public void AScreensAddress_OpensTheFrameworksPage_InTheAdminFrame()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var api = new HttpClient(new StubHandler(Answer)) { BaseAddress = new Uri("http://localhost/") };
        ctx.Services.AddSingleton(api);
        ctx.Services.AddSiteAreas(CodeReviewAreas.Name);
        ctx.Services.GetRequiredService<NavigationManager>().NavigateTo("admin/screens/usage");

        var app = ctx.Render<App>();

        app.WaitForAssertion(() =>
        {
            Assert.That(app.Find(".admin-main h1").TextContent, Is.EqualTo("Usage"));
            Assert.That(app.FindAll(".sidebar a.side-link").Select(link => link.TextContent),
                Has.Some.EqualTo("Evals"));
        }, TimeSpan.FromSeconds(10));
    }

    /// <summary>
    /// F144: the admin's home draws a card for each of the app's admin areas, the overview, the packs and the evals,
    /// in the order it names them.
    /// </summary>
    [Test]
    public void TheAdminsHome_DrawsACardPerArea_InOrder()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var api = new HttpClient(new StubHandler(Answer)) { BaseAddress = new Uri("http://localhost/") };
        ctx.Services.AddSingleton(api);
        ctx.Services.AddSiteAreas(CodeReviewAreas.Name);
        ctx.Services.GetRequiredService<NavigationManager>().NavigateTo("admin");

        var app = ctx.Render<App>();

        app.WaitForAssertion(() =>
            Assert.That(app.FindAll(".admin-card-title").Select(title => title.TextContent),
                Is.EqualTo(new[] { "Overview", "Packs", "Evals" })), TimeSpan.FromSeconds(10));
    }

    /// <summary>The program names the areas the test above draws, through the same method.</summary>
    [Test]
    public void TheProgram_NamesItsAreas()
    {
        var program = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "demos/louis-agent/web/louis-agent.code-review/Program.cs"));

        Assert.Multiple(() =>
        {
            Assert.That(program, Does.Contain("AddSiteAreas(CodeReviewAreas.Name)"));
            Assert.That(program, Does.Not.Contain("new SiteFrames(").And.Not.Contain("new PackViews(")
                .And.Not.Contain("new ChatHandoff("));
        });
    }

    /// <summary>The API's answers: one screen, and no rows.</summary>
    private static HttpResponseMessage Answer(HttpRequestMessage request)
    {
        var path = request.RequestUri!.AbsolutePath;
        if (path.EndsWith("/v1/admin/screens"))
            return Json(new ScreensReply(
            [
                new ScreenReply("usage", "Usage", "What was used.", [new ScreenColumnReply("Name", "text")],
                    Searchable: false)
            ]));

        return path.EndsWith("/rows")
            ? Json(new ScreenPageReply([], 0, 50))
            : new HttpResponseMessage(HttpStatusCode.NotFound);
    }

    /// <summary>A reply as the admin routes write it.</summary>
    private static HttpResponseMessage Json<T>(T reply) =>
        new(HttpStatusCode.OK) { Content = JsonContent.Create(reply, options: AdminJson.SnakeCase) };
}
