using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Screens;
using LouisAgent.Starter.Layout;
using LouisAgent.Starter.Pages;
using LouisAgent.Web.Components.Areas;
using LouisAgent.Web.Components.Chat.Header;
using LouisAgent.Web.Pages.Admin;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Text;
using LouisAgent.Web.Usage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Starter.Tests;

/// <summary>
/// The smallest site on the framework's web: it has a chat and an admin with no code of its own beyond its name, in the
/// framework's colours, which is what a new site starts from.
/// </summary>
public class StarterSiteTests
{
    /// <summary>A context with what the site asks for, over the stub API.</summary>
    private static BunitContext Context()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var settings = new ChatSettings();
        var api = FixtureApi.Client(settings);
        ctx.Services.AddSingleton(settings);
        ctx.Services.AddSingleton(new ChatStore(ctx.JSInterop.JSRuntime, settings));
        ctx.Services.AddSingleton(api);
        ctx.Services.AddSingleton(new CurrencyState(api, ctx.JSInterop.JSRuntime));
        ctx.Services.AddSiteAreas(StarterAreas.Name);
        return ctx;
    }

    /// <summary>Every address a page in this assembly answers to.</summary>
    private static IEnumerable<string> RoutesOf(Assembly assembly) =>
        assembly.GetTypes().SelectMany(type => type.GetCustomAttributes<RouteAttribute>())
            .Select(route => route.Template);

    /// <summary>The chat draws on the framework's frame, with what needs more set up turned off.</summary>
    [Test]
    public void TheChat_DrawsOnTheFrame()
    {
        using var ctx = Context();

        var cut = ctx.Render<ChatPage>();

        cut.WaitForState(() => cut.FindAll("#composer-input").Count > 0, TimeSpan.FromSeconds(10));
        Assert.That(cut.FindAll(".app .sidebar"), Has.Count.EqualTo(1));
        Assert.That(cut.FindComponents<CreditsMenu>(), Is.Empty);
    }

    /// <summary>
    /// A screen's address opens the framework's page, in this site's admin frame, through its router: the areas the
    /// program names (F127.6), with the eval studio's links in the admin's sidebar.
    /// </summary>
    [Test]
    public void AScreensAddress_OpensInTheAdminFrame()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var api = new HttpClient(new StubHandler(Answer)) { BaseAddress = new Uri("http://localhost/") };
        ctx.Services.AddSingleton(api);
        ctx.Services.AddSiteAreas(StarterAreas.Name);
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
    /// The program names its areas through the same method the tests use, so a new site made from it starts with the
    /// builder and no part wired by hand.
    /// </summary>
    [Test]
    public void TheProgram_NamesItsAreas()
    {
        var program = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "demos/starter/web/louis-agent.starter/Program.cs"));

        Assert.Multiple(() =>
        {
            Assert.That(program, Does.Contain("AddSiteAreas(StarterAreas.Name)"));
            Assert.That(program, Does.Not.Contain("new SiteFrames(").And.Not.Contain("new PackViews(")
                .And.Not.Contain("new ChatHandoff("));
        });
    }

    /// <summary>No address is claimed twice, by the site and the framework together.</summary>
    [Test]
    public void NoAddress_IsClaimedTwice()
    {
        var routes = RoutesOf(typeof(App).Assembly).Concat(RoutesOf(typeof(Screen).Assembly)).ToList();

        var claimedTwice = routes.GroupBy(route => route, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1).Select(group => group.Key);
        Assert.That(claimedTwice, Is.Empty);
    }

    /// <summary>It copies none of the framework's rules, and with no colours of its own it has the framework's.</summary>
    [Test]
    public void ItsStyles_AreTheFrameworks()
    {
        var css = SassStylesheet.Of("louis-agent.starter");

        Assert.That(css.Forwards().Order(), Is.EqualTo(css.AreaFiles().Order()));
        Assert.That(css.Forwards(), Is.EqualTo(new[] { "landing/start" }));
        Assert.That(css.FrameworkForwards()[0], Is.EqualTo("web/settings/colours"));
        Assert.That(css.Read(), Does.Contain("--bs-primary: #ffffff"));
    }

    /// <summary>The template says what the guide does, and leaves out what a build makes.</summary>
    [Test]
    public void TheTemplate_SaysWhatTheGuideDoes()
    {
        string template = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "demos/starter/web/louis-agent.starter/.template.config/template.json"));

        Assert.That(template, Does.Contain("\"shortName\": \"louis-site\""));
        Assert.That(template, Does.Contain("\"sourceName\": \"LouisAgent.Starter\""));
        Assert.That(template, Does.Contain("wwwroot/css/**").And.Contain("bin/**").And.Contain("obj/**"));
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
