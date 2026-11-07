using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Screens;
using LouisAgent.CodeReview.Layout;
using LouisAgent.CodeReview.Pages;
using LouisAgent.Web.Api;
using LouisAgent.Web.Components.Admin;
using LouisAgent.Web.Components.Frames;
using LouisAgent.Web.Components.Packs;
using LouisAgent.Web.Pages.Admin;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Usage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.CodeReview.Tests.Pages;

/// <summary>
/// The framework's admin pages reach the code-review app through its router, inside its admin frame, as they reach
/// every site: its controls become admin screens, so this is where they'll open.
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
    /// The site's landing page is the root and the chat lives at /chat, each in its own layout (F109.1): the site's
    /// pages in the site's frame, which the router draws by default, the chat in a bare one.
    /// </summary>
    [Test]
    public void TheRoot_IsTheLanding_AndTheChatIsAtChat()
    {
        var chatRoutes = typeof(ChatPage).GetCustomAttributes<RouteAttribute>().Select(route => route.Template)
            .ToList();
        var chatLayout = typeof(ChatPage).GetCustomAttribute<LayoutAttribute>()?.LayoutType;
        string router = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "demos/louis-agent/web/louis-agent.code-review/App.razor"));

        Assert.Multiple(() =>
        {
            Assert.That(chatRoutes, Does.Not.Contain("/"), "the root is the landing page's");
            Assert.That(chatRoutes, Does.Contain("/chat"));
            Assert.That(chatLayout, Is.EqualTo(typeof(ChatLayout)), "the chat draws without the site's frame");
            Assert.That(router, Does.Contain("DefaultLayout=\"@typeof(SiteLayout)\""),
                "every other page is in the site's frame");
        });
    }

    /// <summary>Opening a screen's address draws the framework's page, in this site's admin frame.</summary>
    [Test]
    public void AScreensAddress_OpensTheFrameworksPage_InTheAdminFrame()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var settings = new ChatSettings();
        ctx.Services.AddSingleton(settings);
        ctx.Services.AddSingleton(new ChatStore(ctx.JSInterop.JSRuntime, settings));
        ctx.Services.AddSingleton(new AgentApi(
            new HttpClient(new StubHandler(Answer)) { BaseAddress = new Uri("http://localhost/") }, settings));
        ctx.Services.AddSingleton(new PackViews());
        ctx.Services.AddSingleton(new SiteFrames(Admin: typeof(AdminLayout), Account: typeof(AccountLayout)));
        ctx.Services.GetRequiredService<NavigationManager>().NavigateTo("admin/screens/usage");

        var app = ctx.Render<App>();

        app.WaitForAssertion(() => Assert.That(app.Find(".admin-main h1").TextContent, Is.EqualTo("Usage")),
            TimeSpan.FromSeconds(10));
    }

    /// <summary>The program sets the same frame the test above draws in.</summary>
    [Test]
    public void TheProgram_SetsItsAdminFrame_AndItsViews()
    {
        var program = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "demos/louis-agent/web/louis-agent.code-review/Program.cs"));

        Assert.That(program,
            Does.Contain("new SiteFrames(Admin: typeof(AdminLayout), Account: typeof(AccountLayout))"));
        Assert.That(program, Does.Contain("GitWebViews.AddTo(views)"));
        Assert.That(program, Does.Contain("FilesWebViews.AddTo(views)"));
        Assert.That(program, Does.Contain("AddSingleton(views)"));
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
            : new HttpResponseMessage(HttpStatusCode.NotFound) { Content = JsonContent.Create(new { }) };
    }

    /// <summary>A reply as the admin routes write it.</summary>
    private static HttpResponseMessage Json<T>(T reply) =>
        new(HttpStatusCode.OK) { Content = JsonContent.Create(reply, options: AdminJson.SnakeCase) };
}
