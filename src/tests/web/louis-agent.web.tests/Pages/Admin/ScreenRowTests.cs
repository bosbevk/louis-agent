using System.Net;
using System.Net.Http.Json;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Screens;
using LouisAgent.Web.Api;
using LouisAgent.Web.Components.Admin;
using LouisAgent.Web.Pages.Admin;
using LouisAgent.Web.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Pages.Admin;

/// <summary>
/// One row of a pack's screen as its own page: it reads the row, draws its form, and reads it again once it's saved
/// rather than trusting what was typed.
/// </summary>
public class ScreenRowTests
{
    /// <summary>The API's answers, and how many times the row was read.</summary>
    private sealed class FakeRow(HttpStatusCode status = HttpStatusCode.OK)
    {
        public int Reads { get; private set; }

        /// <summary>Answers a read with the row, and a save with saved.</summary>
        public HttpResponseMessage Answer(HttpRequestMessage request)
        {
            if (status != HttpStatusCode.OK)
                return new HttpResponseMessage(status) { Content = JsonContent.Create(new { }) };
            if (request.Method == HttpMethod.Put)
                return Json(new ScreenAnswerReply(true, "Saved.", new Dictionary<string, string>()));

            Reads++;
            return Json(new ScreenRecordReply("Melktert",
                [new ScreenFieldReply("name", "Name", "text", "Melktert", false, true, [], [])]));
        }

        /// <summary>A reply as the admin routes write it.</summary>
        private static HttpResponseMessage Json<T>(T reply) =>
            new(HttpStatusCode.OK) { Content = JsonContent.Create(reply, options: AdminJson.SnakeCase) };
    }

    /// <summary>The page over that API.</summary>
    private static IRenderedComponent<ScreenRow> Render(BunitContext ctx, FakeRow api)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var settings = new ChatSettings();
        ctx.Services.AddSingleton(new ChatStore(ctx.JSInterop.JSRuntime, settings));
        ctx.Services.AddSingleton(new AgentApi(
            new HttpClient(new StubHandler(api.Answer)) { BaseAddress = new Uri("http://localhost/") }, settings));

        var page = ctx.Render<ScreenRow>(p => p.Add(c => c.Name, "recipes").Add(c => c.Id, "row-1"));
        page.WaitForState(() => page.FindAll(".screen-page-loading").Count == 0, TimeSpan.FromSeconds(10));
        return page;
    }

    /// <summary>The heading is the row's, with its form and the way back to the list.</summary>
    [Test]
    public void TheRow_ShowsItsForm_AndTheWayBack()
    {
        using var ctx = new BunitContext();

        var page = Render(ctx, new FakeRow());

        Assert.That(page.Find("h1").TextContent, Is.EqualTo("Melktert"));
        Assert.That(page.FindAll(".screen-form .screen-field"), Has.Count.EqualTo(1));
        Assert.That(page.Find(".screen-back").GetAttribute("href"), Is.EqualTo("admin/screens/recipes"));
    }

    /// <summary>Once saved it reads the row again, rather than trusting what was typed.</summary>
    [Test]
    public void AfterASave_TheRowIsReadAgain()
    {
        using var ctx = new BunitContext();
        var api = new FakeRow();
        var page = Render(ctx, api);

        page.Find(".screen-form").Submit();

        page.WaitForAssertion(() => Assert.That(api.Reads, Is.EqualTo(2)), TimeSpan.FromSeconds(10));
    }

    /// <summary>A refused key shows the box to enter one.</summary>
    [Test]
    public void ARefusedKey_ShowsTheGate()
    {
        using var ctx = new BunitContext();

        var page = Render(ctx, new FakeRow(HttpStatusCode.Unauthorized));

        Assert.That(page.FindComponents<AdminGate>(), Has.Count.EqualTo(1));
        Assert.That(page.FindAll(".admin-key"), Has.Count.EqualTo(1));
    }

    /// <summary>A row that's gone says so, and draws no form.</summary>
    [Test]
    public void ARowThatsGone_SaysSo()
    {
        using var ctx = new BunitContext();

        var page = Render(ctx, new FakeRow(HttpStatusCode.NotFound));

        Assert.That(page.FindComponents<AdminGate>(), Has.Count.EqualTo(1));
        Assert.That(page.FindAll(".screen-form"), Is.Empty);
    }
}
