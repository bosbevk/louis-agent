using System.Net;
using System.Text;
using LouisAgent.Puls.Components.Admin.Head;
using LouisAgent.Testing;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Chats;
using LouisAgent.Web.Shared.Storage;
using Microsoft.Extensions.DependencyInjection;
using ReportsPage = LouisAgent.Puls.Pages.Admin.Data.Reports;

namespace LouisAgent.Puls.Tests.Pages.Admin.Data;

/// <summary>
/// F35.7: the Reports page shows a card for each report a pack keeps, as a table, knowing nothing about any pack. An
/// empty table says so, and a refused key shows the box to enter one.
/// </summary>
public class ReportsPageTests
{
    /// <summary>One report with an all-days row and a day, and one with nothing counted yet.</summary>
    private const string TwoReports = """
        {"reports":[
          {"name":"recipe-library","description":"What the library did.","columns":["Kept","Offered"],
           "rows":[{"label":"All days","counts":[11,22]},{"label":"2026-11-06","counts":[1,2]}]},
          {"name":"visits","description":"Who came.","columns":["Visits"],"rows":[]}
        ]}
        """;

    /// <summary>Each report is a card with its name and what it is, its columns, and its rows in order.</summary>
    [Test]
    public void AReport_IsACard_WithItsTable()
    {
        using var ctx = new BunitContext();
        var cut = Render(ctx, HttpStatusCode.OK, TwoReports);

        cut.WaitForAssertion(() => Assert.That(cut.FindAll(".report-card"), Has.Count.EqualTo(2)),
            TimeSpan.FromSeconds(10));
        var card = cut.FindAll(".report-card")[0];
        Assert.That(card.TextContent, Does.Contain("recipe-library").And.Contain("What the library did."));
        Assert.That(card.QuerySelectorAll("th").Select(th => th.TextContent.Trim()),
            Is.EqualTo(new[] { "", "Kept", "Offered" }));
        var rows = card.QuerySelectorAll(".report-row").ToList();
        Assert.That(rows[0].TextContent, Does.Contain("All days").And.Contain("11").And.Contain("22"));
        Assert.That(rows[1].TextContent, Does.Contain("2026-11-06"));
    }

    /// <summary>A report with no rows says nothing's been counted, rather than showing an empty table.</summary>
    [Test]
    public void AnEmptyReport_SaysSo()
    {
        using var ctx = new BunitContext();
        var cut = Render(ctx, HttpStatusCode.OK, TwoReports);

        cut.WaitForAssertion(() => Assert.That(cut.FindAll(".report-card"), Has.Count.EqualTo(2)),
            TimeSpan.FromSeconds(10));
        var empty = cut.FindAll(".report-card")[1];
        Assert.That(empty.TextContent, Does.Contain("Nothing counted yet"));
        Assert.That(empty.QuerySelectorAll("table"), Is.Empty);
    }

    /// <summary>An instance with no reports says that, rather than showing an empty page.</summary>
    [Test]
    public void NoReports_SaysSo()
    {
        using var ctx = new BunitContext();
        var cut = Render(ctx, HttpStatusCode.OK, """{"reports":[]}""");

        cut.WaitForAssertion(() => Assert.That(cut.Markup, Does.Contain("No pack on this instance keeps a report")),
            TimeSpan.FromSeconds(10));
    }

    /// <summary>A refused key shows the box to enter one, and no numbers.</summary>
    [Test]
    public void ARefusedKey_ShowsTheGate()
    {
        using var ctx = new BunitContext();
        var cut = Render(ctx, HttpStatusCode.Unauthorized, """{"type":"error","error":{"type":"authentication_error","message":"No key."}}""");

        cut.WaitForAssertion(() => Assert.That(cut.FindComponents<AdminGate>(), Has.Count.EqualTo(1)),
            TimeSpan.FromSeconds(10));
        Assert.That(cut.FindAll(".report-card"), Is.Empty);
    }

    /// <summary>The page over an API that answers the route with this.</summary>
    private static IRenderedComponent<ReportsPage> Render(BunitContext ctx, HttpStatusCode status, string answer)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var handler = new StubHandler(request => request.RequestUri!.AbsolutePath.EndsWith("/v1/admin/reports")
            ? new HttpResponseMessage(status)
            {
                Content = new StringContent(answer, Encoding.UTF8, "application/json")
            }
            : new HttpResponseMessage(HttpStatusCode.NotFound));

        var settings = new ChatSettings();
        ctx.Services.AddSingleton(settings);
        ctx.Services.AddSingleton(new AgentApi(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") },
            settings));
        ctx.Services.AddSingleton(new ChatStore(ctx.JSInterop.JSRuntime, settings));
        return ctx.Render<ReportsPage>();
    }
}
