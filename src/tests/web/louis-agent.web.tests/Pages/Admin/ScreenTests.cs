using System.Net;
using System.Net.Http.Json;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Screens;
using LouisAgent.Web.Api;
using LouisAgent.Web.Components.Admin;
using LouisAgent.Web.Components.Packs;
using LouisAgent.Web.Components.Screens.Table;
using LouisAgent.Web.Pages.Admin;
using LouisAgent.Web.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Pages.Admin;

/// <summary>
/// The one page every pack's screen opens on: it asks the API for the description, then hands it to whichever component
/// draws that screen, and it knows no pack.
/// </summary>
public class ScreenTests
{
    private static readonly ScreensReply TwoScreens = new(
    [
        new ScreenReply("usage", "Usage", "What was used.", [new ScreenColumnReply("Name", "text")], Searchable: true),
        new ScreenReply("visits", "Visits", "Who came.", [new ScreenColumnReply("Name", "text")], Searchable: false)
    ]);

    /// <summary>A pack's own view: it says which screen it was handed.</summary>
    private sealed class OwnView : ComponentBase, IScreenView
    {
        [Parameter] public ScreenReply Screen { get; set; } = null!;

        /// <inheritdoc />
        protected override void BuildRenderTree(RenderTreeBuilder builder) =>
            builder.AddMarkupContent(0, $"<p class=\"own-view\">{Screen.Name}</p>");
    }

    /// <summary>The page over a fake API answering the list with these, and with no rows for any screen.</summary>
    private static IRenderedComponent<Screen> Render(BunitContext ctx, string name, PackViews? views = null,
        HttpStatusCode status = HttpStatusCode.OK)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var settings = new ChatSettings();
        ctx.Services.AddSingleton(new ChatStore(ctx.JSInterop.JSRuntime, settings));
        ctx.Services.AddSingleton(views ?? new PackViews());
        ctx.Services.AddSingleton(new AgentApi(new HttpClient(new StubHandler(request => Answer(request, status)))
        {
            BaseAddress = new Uri("http://localhost/")
        }, settings));

        var page = ctx.Render<Screen>(p => p.Add(c => c.Name, name));
        page.WaitForState(() => page.FindAll(".screen-page-loading").Count == 0, TimeSpan.FromSeconds(10));
        return page;
    }

    /// <summary>The list, or an empty page of rows, or what the status says.</summary>
    private static HttpResponseMessage Answer(HttpRequestMessage request, HttpStatusCode status)
    {
        if (status != HttpStatusCode.OK)
            return new HttpResponseMessage(status) { Content = JsonContent.Create(new { }) };

        var rows = request.RequestUri!.AbsolutePath.EndsWith("/rows");
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = rows
                ? JsonContent.Create(new ScreenPageReply([], 0, 50), options: AdminJson.SnakeCase)
                : JsonContent.Create(TwoScreens, options: AdminJson.SnakeCase)
        };
    }

    /// <summary>The heading and the sentence are the pack's, and the plain table is the default.</summary>
    [Test]
    public void AScreen_ShowsItsHeading_AndThePlainTable()
    {
        using var ctx = new BunitContext();

        var page = Render(ctx, "usage");

        Assert.That(page.Find("h1").TextContent, Is.EqualTo("Usage"));
        Assert.That(page.Find(".page-head p").TextContent, Is.EqualTo("What was used."));
        Assert.That(page.FindComponents<ScreenTable>().Single().Instance.Screen.Name, Is.EqualTo("usage"));
    }

    /// <summary>A registered view draws instead, handed the description.</summary>
    [Test]
    public void ARegisteredView_DrawsInstead()
    {
        using var ctx = new BunitContext();

        var page = Render(ctx, "usage", new PackViews().UseScreen<OwnView>("usage"));

        Assert.That(page.Find(".own-view").TextContent, Is.EqualTo("usage"));
        Assert.That(page.FindComponents<ScreenTable>(), Is.Empty);
    }

    /// <summary>An address written by hand can differ in case, and still finds its screen.</summary>
    [Test]
    public void TheNamesCase_DoesNotMatter()
    {
        using var ctx = new BunitContext();

        var page = Render(ctx, "USAGE");

        Assert.That(page.Find("h1").TextContent, Is.EqualTo("Usage"));
    }

    /// <summary>A screen no pack offers says so, and draws nothing.</summary>
    [Test]
    public void AnUnknownScreen_SaysSo()
    {
        using var ctx = new BunitContext();

        var page = Render(ctx, "nothing");

        Assert.That(page.Find(".screen-unknown").TextContent, Does.Contain("nothing"));
        Assert.That(page.FindComponents<ScreenTable>(), Is.Empty);
    }

    /// <summary>A refused key shows the box to enter one.</summary>
    [Test]
    public void ARefusedKey_ShowsTheGate()
    {
        using var ctx = new BunitContext();

        var page = Render(ctx, "usage", status: HttpStatusCode.Unauthorized);

        Assert.That(page.FindComponents<AdminGate>(), Has.Count.EqualTo(1));
        Assert.That(page.FindAll(".admin-key"), Has.Count.EqualTo(1));
    }

    /// <summary>Opening another screen on the same page shows that one.</summary>
    [Test]
    public void AnotherScreen_ReplacesThisOne()
    {
        using var ctx = new BunitContext();
        var page = Render(ctx, "usage");

        page.Render(p => p.Add(c => c.Name, "visits"));

        page.WaitForAssertion(() => Assert.That(page.Find("h1").TextContent, Is.EqualTo("Visits")),
            TimeSpan.FromSeconds(10));
    }
}
