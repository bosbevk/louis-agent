using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Screens;
using LouisAgent.Puls.Admin.Reviews;
using LouisAgent.Puls.Admin.Reviews.Queue;
using LouisAgent.Puls.Layout;
using LouisAgent.Web.Api;
using LouisAgent.Web.Components.Admin;
using LouisAgent.Web.Components.Packs;
using LouisAgent.Web.Pages.Admin;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Usage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Puls.Tests.Pages;

/// <summary>
/// The framework's admin pages reach this site through its router, inside its admin frame, and share its addresses: a
/// clash, or a router not told about them, would only show when someone opened the address.
/// </summary>
public class SiteRoutesTests
{
    /// <summary>Every address a page in this assembly answers to.</summary>
    private static IEnumerable<string> RoutesOf(Assembly assembly) =>
        assembly.GetTypes().SelectMany(type => type.GetCustomAttributes<RouteAttribute>()).Select(route => route.Template);

    /// <summary>No address is claimed twice, by the site and the framework together.</summary>
    [Test]
    public void NoAddress_IsClaimedTwice()
    {
        var routes = RoutesOf(typeof(App).Assembly).Concat(RoutesOf(typeof(Screen).Assembly)).ToList();

        var claimedTwice = routes.GroupBy(route => route, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1).Select(group => group.Key);
        Assert.That(claimedTwice, Is.Empty);
    }

    /// <summary>Opening a screen's address draws the framework's page, in the site's admin frame.</summary>
    [Test]
    public void AScreensAddress_OpensTheFrameworksPage_InTheAdminFrame()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var settings = new ChatSettings();
        ctx.Services.AddSingleton(settings);
        ctx.Services.AddSingleton(new ChatStore(ctx.JSInterop.JSRuntime, settings));
        ctx.Services.AddSingleton(new AgentApi(new HttpClient(new StubHandler(Answer))
        {
            BaseAddress = new Uri("http://localhost/")
        }, settings));
        ctx.Services.AddSingleton(new ReviewerState(ctx.JSInterop.JSRuntime));
        ctx.Services.AddSingleton(new ReviewQueueState());
        ctx.Services.AddSingleton(new PackViews());
        ctx.Services.AddSingleton(new SiteFrames(Admin: typeof(AdminLayout)));
        ctx.Services.GetRequiredService<NavigationManager>().NavigateTo("admin/screens/usage");

        var app = ctx.Render<App>();

        app.WaitForAssertion(() => Assert.That(app.Find(".admin-main h1").TextContent, Is.EqualTo("Usage")),
            TimeSpan.FromSeconds(10));
    }

    /// <summary>The program sets the same frame the test above draws in.</summary>
    [Test]
    public void TheProgram_SetsItsAdminFrame_AndItsViews()
    {
        var program = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "demos/puls/web/louis-agent.puls/Program.cs"));

        Assert.That(program, Does.Contain("new SiteFrames(Admin: typeof(AdminLayout))"));
        Assert.That(program, Does.Contain("AddSingleton(new PackViews())"));
    }

    /// <summary>The API's answers: one screen, no rows, and nothing running or to review.</summary>
    private static HttpResponseMessage Answer(HttpRequestMessage request)
    {
        var path = request.RequestUri!.AbsolutePath;
        if (path.EndsWith("/v1/admin/screens"))
            return Json(new ScreensReply([new ScreenReply("usage", "Usage", "What was used.", [], Searchable: false)]));

        return path.EndsWith("/rows")
            ? Json(new ScreenPageReply([], 0, 50))
            : new HttpResponseMessage(HttpStatusCode.NotFound) { Content = JsonContent.Create(new { }) };
    }

    /// <summary>A reply as the admin routes write it.</summary>
    private static HttpResponseMessage Json<T>(T reply) =>
        new(HttpStatusCode.OK) { Content = JsonContent.Create(reply, options: AdminJson.SnakeCase) };
}
