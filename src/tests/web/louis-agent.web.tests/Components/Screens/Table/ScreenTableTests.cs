using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Screens;
using LouisAgent.Web.Api;
using LouisAgent.Web.Components.Admin;
using LouisAgent.Web.Components.Screens.Table;
using LouisAgent.Web.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Screens.Table;

/// <summary>
/// One screen's rows as a plain table, drawn from what the pack described: each kind of cell, search, paging, and what
/// it says when there's nothing to show. It knows no pack.
/// </summary>
public class ScreenTableTests
{
    private const string Date = "2026-11-06T10:00:00Z";

    /// <summary>A screen with one column of each kind.</summary>
    private static readonly ScreenReply EveryKind = new("usage", "Usage", "What was used.",
    [
        new ScreenColumnReply("Name", "text"),
        new ScreenColumnReply("Count", "number"),
        new ScreenColumnReply("Page", "link"),
        new ScreenColumnReply("Read", "date")
    ], Searchable: true);

    /// <summary>The API's answer to any rows request, and every address it was asked.</summary>
    private sealed class FakeRows(params ScreenRowReply[] rows)
    {
        public List<string> Asked { get; } = [];
        public long Total { get; set; } = rows.Length;
        public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;

        /// <summary>Answers one request, keeping its address.</summary>
        public HttpResponseMessage Answer(HttpRequestMessage request)
        {
            Asked.Add(request.RequestUri!.PathAndQuery);
            if (Status != HttpStatusCode.OK)
                return new HttpResponseMessage(Status) { Content = JsonContent.Create(new { }) };

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new ScreenPageReply(rows, Total, PageSize: 50), options: AdminJson.SnakeCase)
            };
        }
    }

    /// <summary>The table over a fake API, already showing what it first read.</summary>
    private static IRenderedComponent<ScreenTable> Render(BunitContext ctx, FakeRows api, ScreenReply? screen = null,
        RenderFragment<ScreenPageReply>? above = null)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var settings = new ChatSettings();
        ctx.Services.AddSingleton(new ChatStore(ctx.JSInterop.JSRuntime, settings));
        ctx.Services.AddSingleton(new AgentApi(
            new HttpClient(new StubHandler(api.Answer)) { BaseAddress = new Uri("http://localhost/") }, settings));

        var table = ctx.Render<ScreenTable>(p => p
            .Add(c => c.Screen, screen ?? EveryKind)
            .Add(c => c.Above, above));
        table.WaitForState(() => api.Asked.Count > 0 && table.FindAll(".screen-loading").Count == 0,
            TimeSpan.FromSeconds(10));
        return table;
    }

    /// <summary>The cells of the table's first row.</summary>
    private static List<AngleSharp.Dom.IElement> FirstRow(IRenderedComponent<ScreenTable> table) =>
        [.. table.FindAll("tbody tr")[0].QuerySelectorAll("td")];

    /// <summary>The headings come from the description, in its order.</summary>
    [Test]
    public void TheHeadings_AreTheColumns()
    {
        using var ctx = new BunitContext();

        var table = Render(ctx, new FakeRows(new ScreenRowReply("a", ["x", "1", null, null])));

        Assert.That(table.FindAll("th").Select(th => th.TextContent), Is.EqualTo(new[] { "Name", "Count", "Page", "Read" }));
    }

    /// <summary>Text is text, and a number lines up on the right.</summary>
    [Test]
    public void TextAndNumbers_DrawAsTheyAre()
    {
        using var ctx = new BunitContext();

        var table = Render(ctx, new FakeRows(new ScreenRowReply("a", ["Apple", "35.00", null, null])));

        var cells = FirstRow(table);
        Snapshots.Match("Components/Screens/Table/ScreenTable.html", table.Markup);
        Assert.That(cells[0].TextContent, Is.EqualTo("Apple"));
        Assert.That(cells[0].ClassList, Does.Not.Contain("num"));
        Assert.That(cells[1].TextContent, Is.EqualTo("35.00"));
        Assert.That(cells[1].ClassList, Does.Contain("num"));
    }

    /// <summary>A link opens in its own tab, and the page it opens can't reach back to this one.</summary>
    [Test]
    public void ALink_OpensInItsOwnTab()
    {
        using var ctx = new BunitContext();

        var cells = FirstRow(Render(ctx, new FakeRows(new ScreenRowReply("a", ["x", "1", "https://example.com/a", null]))));

        var link = cells[2].QuerySelector("a")!;
        Assert.That(link.GetAttribute("href"), Is.EqualTo("https://example.com/a"));
        Assert.That(link.GetAttribute("target"), Is.EqualTo("_blank"));
        Assert.That(link.GetAttribute("rel"), Is.EqualTo("noopener noreferrer"));
    }

    /// <summary>Only a web address becomes a link: what a page said about itself can't run in the admin's tab.</summary>
    [TestCase("javascript:alert(1)")]
    [TestCase("data:text/html,x")]
    [TestCase("not an address")]
    public void WhatIsNotAWebAddress_StaysText(string value)
    {
        using var ctx = new BunitContext();

        var cells = FirstRow(Render(ctx, new FakeRows(new ScreenRowReply("a", ["x", "1", value, null]))));

        Assert.That(cells[2].QuerySelector("a"), Is.Null);
        Assert.That(cells[2].TextContent, Is.EqualTo(value));
    }

    /// <summary>A date is in the reader's own time; one that can't be read shows as it came.</summary>
    [TestCase(Date, true)]
    [TestCase("last week", false)]
    public void ADate_IsInTheReadersTime(string value, bool readable)
    {
        using var ctx = new BunitContext();

        var cells = FirstRow(Render(ctx, new FakeRows(new ScreenRowReply("a", ["x", "1", null, value]))));

        var expected = readable
            ? DateTimeOffset.Parse(value, CultureInfo.InvariantCulture).ToLocalTime()
                .ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
            : value;
        Assert.That(cells[3].TextContent, Is.EqualTo(expected));
    }

    /// <summary>What the pack doesn't know says unknown, in every kind, never 0 or nothing.</summary>
    [Test]
    public void Null_ShowsAsUnknown()
    {
        using var ctx = new BunitContext();

        var cells = FirstRow(Render(ctx, new FakeRows(new ScreenRowReply("a", [null, null, null, null]))));

        Assert.That(cells.Select(cell => cell.TextContent), Is.All.EqualTo("unknown"));
    }

    /// <summary>A search asks again from the first page, its words escaped.</summary>
    [Test]
    public void ASearch_AsksAgain_FromTheFirstPage()
    {
        using var ctx = new BunitContext();
        var api = new FakeRows(new ScreenRowReply("a", ["x", "1", null, null])) { Total = 120 };
        var table = Render(ctx, api);
        table.Find(".screen-next").Click();
        table.WaitForState(() => api.Asked.Count == 2, TimeSpan.FromSeconds(10));

        table.Find(".screen-search input").Change("melk tert");
        table.Find(".screen-search").Submit();
        table.WaitForState(() => api.Asked.Count == 3, TimeSpan.FromSeconds(10));

        Assert.That(api.Asked[^1], Is.EqualTo("/v1/admin/screens/usage/rows?page=1&search=melk%20tert"));
    }

    /// <summary>A screen that can't be searched has no box.</summary>
    [Test]
    public void AScreenThatCannotBeSearched_HasNoBox()
    {
        using var ctx = new BunitContext();

        var table = Render(ctx, new FakeRows(), EveryKind with { Searchable = false });

        Assert.That(table.FindAll(".screen-search"), Is.Empty);
    }

    /// <summary>Paging goes on and back, says where you are, and stops at each end.</summary>
    [Test]
    public void Paging_GoesOnAndBack_AndStopsAtEachEnd()
    {
        using var ctx = new BunitContext();
        var api = new FakeRows(new ScreenRowReply("a", ["x", "1", null, null])) { Total = 60 };
        var table = Render(ctx, api);
        Assert.That(api.Asked[0], Is.EqualTo("/v1/admin/screens/usage/rows?page=1"));
        Assert.That(table.Find(".screen-previous").HasAttribute("disabled"), Is.True);
        Assert.That(table.Find(".screen-paging").TextContent, Does.Contain("Page 1 of 2").And.Contain("60"));

        table.Find(".screen-next").Click();
        table.WaitForAssertion(() => Assert.That(table.Find(".screen-next").HasAttribute("disabled"), Is.True),
            TimeSpan.FromSeconds(10));

        Assert.That(api.Asked[^1], Is.EqualTo("/v1/admin/screens/usage/rows?page=2"));
        table.Find(".screen-previous").Click();
        table.WaitForState(() => api.Asked.Count == 3, TimeSpan.FromSeconds(10));
        Assert.That(api.Asked[^1], Is.EqualTo("/v1/admin/screens/usage/rows?page=1"));
    }

    /// <summary>All of it on one page needs no buttons.</summary>
    [Test]
    public void OnePage_HasNoPaging()
    {
        using var ctx = new BunitContext();

        var table = Render(ctx, new FakeRows(new ScreenRowReply("a", ["x", "1", null, null])));

        Assert.That(table.FindAll(".screen-paging"), Is.Empty);
    }

    /// <summary>With no rows it says so, and shows no empty table.</summary>
    [Test]
    public void NoRows_SaysSo()
    {
        using var ctx = new BunitContext();

        var table = Render(ctx, new FakeRows());

        Assert.That(table.Find(".screen-empty").TextContent, Does.Contain("Nothing here yet"));
        Assert.That(table.FindAll("table"), Is.Empty);
    }

    /// <summary>A search that finds nothing says that instead, and keeps the box.</summary>
    [Test]
    public void ASearchThatFindsNothing_SaysSo()
    {
        using var ctx = new BunitContext();
        var api = new FakeRows();
        var table = Render(ctx, api);

        table.Find(".screen-search input").Change("melk");
        table.Find(".screen-search").Submit();
        table.WaitForState(() => api.Asked.Count == 2, TimeSpan.FromSeconds(10));

        table.WaitForAssertion(() => Assert.That(table.Find(".screen-empty").TextContent, Does.Contain("Nothing matches")),
            TimeSpan.FromSeconds(10));
        Assert.That(table.FindAll(".screen-search"), Has.Count.EqualTo(1));
    }

    /// <summary>What a view puts above the table reads the page; with no rows it isn't drawn.</summary>
    [Test]
    public void Above_ReadsThePage_AndIsAbsentWithNoRows()
    {
        RenderFragment<ScreenPageReply> above = page => builder =>
            builder.AddMarkupContent(0, $"<p class=\"above\">Of {page.Total}</p>");
        using var withRows = new BunitContext();
        using var withNone = new BunitContext();

        var shown = Render(withRows, new FakeRows(new ScreenRowReply("a", ["x", "1", null, null])), above: above);
        var empty = Render(withNone, new FakeRows(), above: above);

        Assert.That(shown.Find(".above").TextContent, Is.EqualTo("Of 1"));
        Assert.That(empty.FindAll(".above"), Is.Empty);
    }

    /// <summary>Another screen in the same table starts over: the page opening the next one reuses this component.</summary>
    [Test]
    public void AnotherScreen_StartsOver()
    {
        using var ctx = new BunitContext();
        var api = new FakeRows(new ScreenRowReply("a", ["x", "1", null, null])) { Total = 60 };
        var table = Render(ctx, api);
        table.Find(".screen-next").Click();
        table.WaitForState(() => api.Asked.Count == 2, TimeSpan.FromSeconds(10));

        table.Render(p => p.Add(c => c.Screen, EveryKind with { Name = "visits" }));
        table.WaitForState(() => api.Asked.Count == 3, TimeSpan.FromSeconds(10));

        Assert.That(api.Asked[^1], Is.EqualTo("/v1/admin/screens/visits/rows?page=1"));
    }

    /// <summary>A refused key shows the box to enter one, and no table.</summary>
    [Test]
    public void ARefusedKey_ShowsTheGate()
    {
        using var ctx = new BunitContext();

        var table = Render(ctx, new FakeRows { Status = HttpStatusCode.Unauthorized });

        Assert.That(table.FindComponents<AdminGate>(), Has.Count.EqualTo(1));
        Assert.That(table.FindAll(".admin-key"), Has.Count.EqualTo(1));
        Assert.That(table.FindAll("table"), Is.Empty);
    }
}
