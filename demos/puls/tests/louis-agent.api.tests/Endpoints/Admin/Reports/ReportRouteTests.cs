using System.Net;
using System.Net.Http.Json;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Reports;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;

namespace LouisAgent.Api.Tests.Endpoints.Admin.Reports;

/// <summary>
/// The packs' reports over the API (F35.7), behind the admin key: what a pack lists comes back as its table, and the
/// route knows nothing of which pack it came from.
/// </summary>
public class ReportRouteTests
{
    private const string AdminKey = "admin-secret";

    /// <summary>The API with a pack that lists these reports, and a client carrying the admin key.</summary>
    private static async Task<(WebApplication App, HttpClient Client)> StartAsync(params FakeReport[] reports)
    {
        var app = ApiTestHost.Build(adminKey: AdminKey,
            morePacks: reports.Length == 0 ? null : [new FakePack("reporter", toolsets: [], reports: reports)]);
        await app.StartAsync();
        var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add("x-api-key", AdminKey);
        return (app, client);
    }

    /// <summary>A pack's report comes back whole: its name, what it is, its columns and its rows.</summary>
    [Test]
    public async Task APacksReport_ComesBackAsItsTable()
    {
        var (app, client) = await StartAsync(new FakeReport("usage", 3, 4));
        await using var _ = app;

        var reply = await client.GetFromJsonAsync<ReportsReply>("/v1/admin/reports", AdminJson.SnakeCase);

        var report = reply!.Reports.Single();
        Assert.That(report.Name, Is.EqualTo("usage"));
        Assert.That(report.Description, Is.EqualTo("The usage report"));
        Assert.That(report.Columns, Is.EqualTo(new[] { "Column 1", "Column 2" }));
        Assert.That(report.Rows.Single().Label, Is.EqualTo("All days"));
        Assert.That(report.Rows.Single().Counts, Is.EqualTo(new long[] { 3, 4 }));
    }

    /// <summary>Several reports come back in the order the packs list them.</summary>
    [Test]
    public async Task SeveralReports_KeepTheirOrder()
    {
        var (app, client) = await StartAsync(new FakeReport("second", 1), new FakeReport("first", 2));
        await using var _ = app;

        var reply = await client.GetFromJsonAsync<ReportsReply>("/v1/admin/reports", AdminJson.SnakeCase);

        Assert.That(reply!.Reports.Select(r => r.Name), Is.EqualTo(new[] { "second", "first" }));
    }

    /// <summary>With no pack listing one, the answer is an empty list, not an error.</summary>
    [Test]
    public async Task WithNoReports_TheListIsEmpty()
    {
        var (app, client) = await StartAsync();
        await using var _ = app;

        var reply = await client.GetFromJsonAsync<ReportsReply>("/v1/admin/reports", AdminJson.SnakeCase);

        Assert.That(reply!.Reports, Is.Empty);
    }

    /// <summary>What a sold endpoint's users did isn't for anyone to read: no key, no numbers.</summary>
    [Test]
    public async Task WithoutTheAdminKey_ItIsRefused()
    {
        var (app, _) = await StartAsync(new FakeReport("usage", 3));
        await using var __ = app;

        var noKey = await app.GetTestClient().GetAsync("/v1/admin/reports");

        Assert.That(noKey.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
}
