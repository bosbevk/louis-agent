using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Screens;
using LouisAgent.Web.Evals.Admin.Reviews;
using LouisAgent.Web.Evals.Admin.Reviews.Queue;
using LouisAgent.Web.Components.Areas;
using LouisAgent.Web.Pages.Admin;
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
    /// Opening a screen's address draws the framework's page in the site's admin frame: the areas the program names
    /// (F127.6), the eval studio's links in the admin's sidebar once, drawn by the framework.
    /// </summary>
    [Test]
    public void AScreensAddress_OpensTheFrameworksPage_InTheAdminFrame()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var api = new HttpClient(new StubHandler(Answer)) { BaseAddress = new Uri("http://localhost/") };
        ctx.Services.AddSingleton(api);
        ctx.Services.AddSingleton(new ReviewerState(ctx.JSInterop.JSRuntime));
        ctx.Services.AddSingleton(new ReviewQueueState());
        ctx.Services.AddSiteAreas(PulsAreas.Name);
        ctx.Services.GetRequiredService<NavigationManager>().NavigateTo("admin/screens/usage");

        var app = ctx.Render<App>();

        app.WaitForAssertion(() =>
        {
            Assert.That(app.Find(".admin-main h1").TextContent, Is.EqualTo("Usage"));
            Assert.That(app.FindAll(".sidebar a.side-link").Count(link => link.TextContent.StartsWith("Evals")),
                Is.EqualTo(1));
        }, TimeSpan.FromSeconds(10));
    }

    /// <summary>
    /// F144: the admin's home draws a card for each of Puls's admin areas, the overview, the packs and the evals, in
    /// the order it names them.
    /// </summary>
    [Test]
    public void TheAdminsHome_DrawsACardPerArea_InOrder()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var api = new HttpClient(new StubHandler(Answer)) { BaseAddress = new Uri("http://localhost/") };
        ctx.Services.AddSingleton(api);
        ctx.Services.AddSingleton(new ReviewerState(ctx.JSInterop.JSRuntime));
        ctx.Services.AddSingleton(new ReviewQueueState());
        ctx.Services.AddSiteAreas(PulsAreas.Name);
        ctx.Services.GetRequiredService<NavigationManager>().NavigateTo("admin");

        var app = ctx.Render<App>();

        app.WaitForAssertion(() =>
            Assert.That(app.FindAll(".admin-card-title").Select(title => title.TextContent),
                Is.EqualTo(new[] { "Overview", "Packs", "Evals" })), TimeSpan.FromSeconds(10));
    }

    /// <summary>
    /// The program names its areas through the same method the tests use, its own words registered first so they
    /// stay (F61.2: the account pages, in Puls's words).
    /// </summary>
    [Test]
    public void TheProgram_NamesItsAreas_InItsOwnWords()
    {
        var program = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "demos/puls/web/louis-agent.puls/Program.cs"));

        Assert.Multiple(() =>
        {
            Assert.That(program, Does.Contain("AddSiteAreas(PulsAreas.Name)"));
            Assert.That(program, Does.Not.Contain("new SiteFrames(").And.Not.Contain("new PackViews("));
            Assert.That(program.IndexOf("AddSingleton(PulsAccountText.Afrikaans)", StringComparison.Ordinal),
                Is.InRange(0, program.IndexOf("AddSiteAreas(", StringComparison.Ordinal)));
        });
    }

    /// <summary>The API's answers: one screen, no rows, and nothing running or to review.</summary>
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
