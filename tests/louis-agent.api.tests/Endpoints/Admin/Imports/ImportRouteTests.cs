using System.Net;
using System.Net.Http.Json;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Imports;
using LouisAgent.Choices;
using LouisAgent.Packs.ShopPrices;
using LouisAgent.Packs.ShopPrices.Prices.StatsSa.Sheets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;

namespace LouisAgent.Api.Tests.Endpoints.Admin.Imports;

/// <summary>
/// The packs' uploads over the API (F55), behind the admin key: the list names the shop-prices upload and its two
/// files, a preview reads Stats SA's trimmed release, and a save on a host with no price store says it can't keep it.
/// The test host carries Puls's packs, with no MongoDB.
/// </summary>
public class ImportRouteTests
{
    private const string AdminKey = "admin-secret";

    private static readonly string Upload = ShopPricesToolset.ShopPrices.Wire();

    private static ImportRequest Release() => new(
    [
        File(StatsSaSheet.AllUrban, "statssa-all-urban.xlsx"),
        File(StatsSaSheet.Provinces, "statssa-provinces.xlsx"),
    ]);

    private static ImportFileBody File(StatsSaSheet sheet, string fixture) => new(sheet.Wire(), fixture,
        System.IO.File.ReadAllBytes(Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", fixture)));

    private static async Task<(WebApplication App, HttpClient Client)> StartAsync()
    {
        var app = ApiTestHost.Build(adminKey: AdminKey);
        await app.StartAsync();
        var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add("x-api-key", AdminKey);
        return (app, client);
    }

    /// <summary>The list names the upload and the two files it takes.</summary>
    [Test]
    public async Task TheList_NamesTheUploadAndItsFiles()
    {
        var (app, client) = await StartAsync();
        await using var _ = app;

        var reply = await client.GetFromJsonAsync<ImportsReply>("/v1/admin/imports", AdminJson.SnakeCase);

        var row = reply!.Imports.Single(i => i.Name == Upload);
        Assert.That(row.Description, Does.Contain("Stats SA"));
        Assert.That(row.Files.Select(f => f.Name), Is.EqualTo(new[] { StatsSaSheet.AllUrban.Wire(), StatsSaSheet.Provinces.Wire() }));
    }

    /// <summary>The preview reads the files and says what saving them would store.</summary>
    [Test]
    public async Task ThePreview_ReadsTheFiles()
    {
        var (app, client) = await StartAsync();
        await using var _ = app;

        var response = await client.PostAsJsonAsync($"/v1/admin/imports/{Upload}/preview", Release(), AdminJson.SnakeCase);
        var report = await response.Content.ReadFromJsonAsync<ImportReportReply>(AdminJson.SnakeCase);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(report!.Period, Is.EqualTo("June 2026 to August 2026"));
        Assert.That(report.Saved, Is.False);
        Assert.That(report.Facts.Select(f => f.Label), Does.Contain("New months"));
    }

    /// <summary>With no price store set, a save keeps nothing and says why.</summary>
    [Test]
    public async Task ASave_WithNoStore_SaysItCantKeepThem()
    {
        var (app, client) = await StartAsync();
        await using var _ = app;

        var response = await client.PostAsJsonAsync($"/v1/admin/imports/{Upload}/save", Release(), AdminJson.SnakeCase);
        var report = await response.Content.ReadFromJsonAsync<ImportReportReply>(AdminJson.SnakeCase);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(report!.Saved, Is.False);
        Assert.That(report.Problems.Single(), Does.Contain("SHOP_PRICES_MONGO_URL"));
    }

    /// <summary>An upload no pack takes is a 404; no files is a 400; without the key it is refused.</summary>
    [Test]
    public async Task AnUnknownUpload_NoFiles_OrNoKey_AreRefused()
    {
        var (app, client) = await StartAsync();
        await using var _ = app;

        var unknown = await client.PostAsJsonAsync("/v1/admin/imports/rent-prices/preview", Release(), AdminJson.SnakeCase);
        var empty = await client.PostAsJsonAsync($"/v1/admin/imports/{Upload}/preview", new ImportRequest([]), AdminJson.SnakeCase);
        var anonymous = app.GetTestClient();
        var noKey = await anonymous.GetAsync("/v1/admin/imports");

        Assert.That(unknown.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(await unknown.Content.ReadAsStringAsync(), Does.Contain("rent-prices"));
        Assert.That(empty.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(noKey.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
}
