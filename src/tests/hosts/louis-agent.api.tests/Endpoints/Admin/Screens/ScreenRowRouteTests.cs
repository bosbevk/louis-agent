using System.Net;
using System.Net.Http.Json;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Screens;
using LouisAgent.Composition.Packs.Screens.Acting;
using LouisAgent.Composition.Packs.Screens.Editing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;

namespace LouisAgent.Api.Tests.Endpoints.Admin.Screens;

/// <summary>
/// A row as a form, over the API: the fields come back in the pack's order, the pack checks what's saved, and a refusal
/// names its field. A screen that doesn't say its rows can be edited has no form.
/// </summary>
public class ScreenRowRouteTests
{
    private const string AdminKey = "admin-secret";
    private const string Row = "/v1/admin/screens/usage/rows/a";

    private static readonly ScreenRecord Form = new("A recipe",
    [
        new ScreenField("name", "Name", ScreenFieldKind.Text, "Melktert", Required: true),
        new ScreenField("page", "Page", ScreenFieldKind.Link, "https://example.com/a", ReadOnly: true),
        new ScreenField("steps", "Steps", ScreenFieldKind.Rows)
        {
            RowColumns = ["Section", "Text"],
            Rows = [["", "Bak dit"]]
        }
    ]);

    /// <summary>The API with this screen, and a client carrying the admin key.</summary>
    private static async Task<(WebApplication App, HttpClient Client)> StartAsync(FakeScreen screen)
    {
        var app = ApiTestHost.Build(adminKey: AdminKey, morePacks: [new FakePack("screener", toolsets: [], screens: [screen])]);
        await app.StartAsync();
        var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add("x-api-key", AdminKey);
        return (app, client);
    }

    /// <summary>A form saved with these values.</summary>
    private static JsonContent Saved() => JsonContent.Create(new ScreenSaveRequest(
        new Dictionary<string, string?> { ["name"] = "Melktert 2" },
        new Dictionary<string, IReadOnlyList<IReadOnlyList<string>>> { ["steps"] = [["Bo", "Roer"], ["", "Bak"]] }),
        options: AdminJson.SnakeCase);

    /// <summary>The fields come back in order, each with its kind and whether it can change; rows keep their shape.</summary>
    [Test]
    public async Task TheFields_ComeBackInOrder()
    {
        var (app, client) = await StartAsync(new FakeScreen("usage").Editable(Form, ScreenAnswer.Ok("Saved.")));
        await using var _ = app;

        var reply = await client.GetFromJsonAsync<ScreenRecordReply>(Row, AdminJson.SnakeCase);

        Assert.That(reply!.Title, Is.EqualTo("A recipe"));
        Assert.That(reply.Fields.Select(field => field.Name), Is.EqualTo(new[] { "name", "page", "steps" }));
        Assert.That(reply.Fields.Select(field => field.Kind), Is.EqualTo(new[] { "text", "link", "rows" }));
        Assert.That(reply.Fields[0].Required, Is.True);
        Assert.That(reply.Fields[1].ReadOnly, Is.True);
        Assert.That(reply.Fields[2].RowColumns, Is.EqualTo(new[] { "Section", "Text" }));
        Assert.That(reply.Fields[2].Rows.Single(), Is.EqualTo(new[] { "", "Bak dit" }));
    }

    /// <summary>The description says rows can be edited, so the page can link them.</summary>
    [Test]
    public async Task TheDescription_SaysRowsCanBeEdited()
    {
        var (app, client) = await StartAsync(new FakeScreen("usage").Editable(Form, ScreenAnswer.Ok("Saved.")));
        await using var _ = app;

        var reply = await client.GetFromJsonAsync<ScreensReply>("/v1/admin/screens", AdminJson.SnakeCase);

        Assert.That(reply!.Screens.Single().Editable, Is.True);
    }

    /// <summary>What was typed reaches the pack, with the rows in the order sent.</summary>
    [Test]
    public async Task ASave_ReachesThePack_RowsInOrder()
    {
        var screen = new FakeScreen("usage").Editable(Form, ScreenAnswer.Ok("Saved."));
        var (app, client) = await StartAsync(screen);
        await using var _ = app;

        var reply = await client.PutAsync(Row, Saved());

        var answer = await reply.Content.ReadFromJsonAsync<ScreenAnswerReply>(AdminJson.SnakeCase);
        Assert.That(answer!.Done, Is.True);
        Assert.That(screen.LastSave!.Value.RowId, Is.EqualTo("a"));
        Assert.That(screen.LastSave.Value.Values.Values["name"], Is.EqualTo("Melktert 2"));
        Assert.That(screen.LastSave.Value.Values.Rows["steps"].Select(row => row[1]), Is.EqualTo(new[] { "Roer", "Bak" }));
    }

    /// <summary>A refusal comes back naming its field.</summary>
    [Test]
    public async Task ARefusal_NamesItsField()
    {
        var refused = new ScreenAnswer(false, "Not saved.", new Dictionary<string, string> { ["name"] = "A name is required." });
        var (app, client) = await StartAsync(new FakeScreen("usage").Editable(Form, refused));
        await using var _ = app;

        var reply = await client.PutAsync(Row, Saved());

        var answer = await reply.Content.ReadFromJsonAsync<ScreenAnswerReply>(AdminJson.SnakeCase);
        Assert.That(answer!.Done, Is.False);
        Assert.That(answer.FieldErrors["name"], Is.EqualTo("A name is required."));
    }

    /// <summary>A row it doesn't have is not found, to read or to save, and nothing is saved.</summary>
    [Test]
    public async Task AnUnknownRow_IsNotFound()
    {
        var screen = new FakeScreen("usage").Editable(Form, ScreenAnswer.Ok("Saved."));
        var (app, client) = await StartAsync(screen);
        await using var _ = app;

        var read = await client.GetAsync("/v1/admin/screens/usage/rows/zzz");
        var save = await client.PutAsync("/v1/admin/screens/usage/rows/zzz", Saved());

        Assert.That(read.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(save.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(screen.LastSave, Is.Null);
    }

    /// <summary>A screen that doesn't say its rows can be edited has no form.</summary>
    [Test]
    public async Task AReadOnlyScreen_HasNoForm()
    {
        var (app, client) = await StartAsync(new FakeScreen("usage"));
        await using var _ = app;

        var read = await client.GetAsync(Row);

        Assert.That(read.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>No key, no form and no save.</summary>
    [Test]
    public async Task WithoutTheAdminKey_ItIsRefused()
    {
        var screen = new FakeScreen("usage").Editable(Form, ScreenAnswer.Ok("Saved."));
        var (app, _) = await StartAsync(screen);
        await using var __ = app;

        var read = await app.GetTestClient().GetAsync(Row);
        var save = await app.GetTestClient().PutAsync(Row, Saved());

        Assert.That(read.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(save.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(screen.LastSave, Is.Null);
    }
}
