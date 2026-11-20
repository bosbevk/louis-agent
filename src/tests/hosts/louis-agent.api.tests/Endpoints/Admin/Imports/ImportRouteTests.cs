using System.Net;
using System.Net.Http.Json;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Imports;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;

namespace LouisAgent.Api.Tests.Endpoints.Admin.Imports;

/// <summary>
/// The packs' uploads over the API (F55), behind the admin key: the list names a pack's upload and its files, a
/// preview hands the files to the pack and saves nothing, and a save asks the pack to keep them. The upload is the
/// framework's fake (F166.1); what a real pack reads from its files is that pack's own tests'.
/// </summary>
public class ImportRouteTests
{
    private const string AdminKey = "admin-secret";

    /// <summary>The upload the host's one fake pack offers.</summary>
    private const string Upload = "test-upload";

    /// <summary>One file in the fake upload's one slot.</summary>
    private static ImportRequest OneFile() => new([new ImportFileBody("file", "prices.xlsx", [1, 2, 3])]);

    /// <summary>The API behind the admin key, with a pack that offers the fake upload.</summary>
    private static async Task<(WebApplication App, HttpClient Client)> StartAsync()
    {
        var uploads = new FakePack("uploads", imports: [new FakeImport(Upload)]);
        var app = ApiTestHost.Build(adminKey: AdminKey, morePacks: [uploads]);
        await app.StartAsync();
        var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add("x-api-key", AdminKey);
        return (app, client);
    }

    /// <summary>The list names the upload, what it is, and the files it takes.</summary>
    [Test]
    public async Task TheList_NamesTheUploadAndItsFiles()
    {
        var (app, client) = await StartAsync();
        await using var _ = app;

        var reply = await client.GetFromJsonAsync<ImportsReply>("/v1/admin/imports", AdminJson.SnakeCase);

        var row = reply!.Imports.Single(i => i.Name == Upload);
        Assert.That(row.Description, Is.EqualTo($"The {Upload} import"));
        Assert.That(row.Files.Select(f => f.Name), Is.EqualTo(new[] { "file" }));
    }

    /// <summary>
    /// The preview hands the files to the pack and says what saving them would store, saving nothing.
    /// </summary>
    [Test]
    public async Task ThePreview_ReadsTheFiles()
    {
        var (app, client) = await StartAsync();
        await using var _ = app;

        var response =
            await client.PostAsJsonAsync($"/v1/admin/imports/{Upload}/preview", OneFile(), AdminJson.SnakeCase);
        var report = await response.Content.ReadFromJsonAsync<ImportReportReply>(AdminJson.SnakeCase);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(report!.Saved, Is.False);
        Assert.That(report.Changes, Is.EqualTo(1));
        // The fake names each file by its slot, so the pack got the file the request put there.
        Assert.That(report.Facts.Select(f => f.Label), Is.EqualTo(new[] { "file" }));
    }

    /// <summary>A save asks the pack to keep the files, and says it did.</summary>
    [Test]
    public async Task ASave_KeepsTheFiles()
    {
        var (app, client) = await StartAsync();
        await using var _ = app;

        var response =
            await client.PostAsJsonAsync($"/v1/admin/imports/{Upload}/save", OneFile(), AdminJson.SnakeCase);
        var report = await response.Content.ReadFromJsonAsync<ImportReportReply>(AdminJson.SnakeCase);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(report!.Saved, Is.True);
        Assert.That(report.Problems, Is.Empty);
    }

    /// <summary>An upload no pack takes is a 404; no files is a 400; without the key it is refused.</summary>
    [Test]
    public async Task AnUnknownUpload_NoFiles_OrNoKey_AreRefused()
    {
        var (app, client) = await StartAsync();
        await using var _ = app;

        var unknown =
            await client.PostAsJsonAsync("/v1/admin/imports/rent-prices/preview", OneFile(), AdminJson.SnakeCase);
        var empty = await client.PostAsJsonAsync($"/v1/admin/imports/{Upload}/preview", new ImportRequest([]),
            AdminJson.SnakeCase);
        var anonymous = app.GetTestClient();
        var noKey = await anonymous.GetAsync("/v1/admin/imports");

        Assert.That(unknown.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(await unknown.Content.ReadAsStringAsync(), Does.Contain("rent-prices"));
        Assert.That(empty.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(noKey.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
}
