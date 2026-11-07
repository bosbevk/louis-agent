using System.Net;
using System.Text;
using LouisAgent.Puls.Admin.Reviews;
using LouisAgent.Puls.Admin.Reviews.Queue;
using LouisAgent.Puls.Layout;
using LouisAgent.Testing;
using LouisAgent.Web.Api;
using LouisAgent.Web.Components.Packs;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Puls.Tests.Layout;

/// <summary>
/// Puls's admin layout fills the framework's frame with what's its own. The golden was written from the layout before
/// it moved onto the frame, so a change in what the page draws fails here.
/// </summary>
public class AdminLayoutTests
{
    /// <summary>The whole frame draws as it did before it moved.</summary>
    [Test]
    public void ItDrawsAsItDidBeforeTheFrameMoved()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx);

        Snapshots.Match("Layout/AdminLayout.html", cut.Markup);
    }

    /// <summary>The sidebar keeps its links, in their order, to their addresses.</summary>
    [Test]
    public void TheSidebar_KeepsItsLinks_InOrder()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx);

        Assert.That(cut.FindAll(".sidebar .side-link").Select(link => link.GetAttribute("href")), Is.EqualTo(new[]
        {
            "admin", "admin/review", "admin/studio", "admin/prompts", "gesels"
        }));
    }

    /// <summary>The way back goes to the chat, and the page shows in the content column.</summary>
    [Test]
    public void TheWayBack_GoesToTheChat_AndThePageShows()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx);

        Assert.That(cut.Find(".topbar a.icon").GetAttribute("href"), Is.EqualTo("gesels"));
        Assert.That(cut.FindAll(".admin-main .page"), Has.Count.EqualTo(1));
    }

    /// <summary>Nothing running, nothing to review, and no pack offering a screen.</summary>
    private static string AnswerTo(string path)
    {
        if (path.EndsWith("/current")) return "null";

        return path.EndsWith("/screens") ? """{"screens":[]}""" : """{"items":[],"total":0}""";
    }

    /// <summary>The layout with nothing running and nothing to review, once its badge has read the queue.</summary>
    private static IRenderedComponent<AdminLayout> Render(BunitContext ctx)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var handler = new StubHandler(request => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(AnswerTo(request.RequestUri!.AbsolutePath), Encoding.UTF8, "application/json")
        });
        var settings = new ChatSettings();
        ctx.Services.AddSingleton(new ChatStore(ctx.JSInterop.JSRuntime, settings));
        ctx.Services.AddSingleton(new PackViews());
        ctx.Services.AddSingleton(new AgentApi(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") },
            settings));
        ctx.Services.AddSingleton(new ReviewerState(ctx.JSInterop.JSRuntime));
        var state = new ReviewQueueState();
        ctx.Services.AddSingleton(state);

        var cut = ctx.Render<AdminLayout>(parameters => parameters.Add(layout => layout.Body, "<div class=\"page\"></div>"));
        cut.WaitForState(() => state.Count == 0, TimeSpan.FromSeconds(10));
        return cut;
    }
}
