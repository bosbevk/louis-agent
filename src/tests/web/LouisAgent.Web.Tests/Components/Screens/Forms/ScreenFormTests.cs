using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Screens;
using LouisAgent.Web.Api;
using LouisAgent.Web.Components.Screens.Forms;
using LouisAgent.Web.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Screens.Forms;

/// <summary>
/// One row as a form, drawn from what its pack described. The pack has the last word on what's saved, so a refusal
/// shows beside its field, and a field that can't change is never sent.
/// </summary>
public class ScreenFormTests
{
    private static readonly IReadOnlyList<IReadOnlyList<string>> NoRows = [];

    private static ScreenFieldReply Field(string name, string kind, string? value = null, bool readOnly = false,
        bool required = false) => new(name, name.ToUpperInvariant(), kind, value, readOnly, required, [], NoRows);

    private static readonly ScreenRecordReply Form = new("Melktert",
    [
        Field("name", "text", "Melktert", required: true),
        Field("minutes", "number", "45"),
        Field("image", "link", "https://example.com/a.jpg"),
        Field("page", "link", "https://example.com/a", readOnly: true),
        new ScreenFieldReply("steps", "STEPS", "rows", null, false, false, ["Section", "Text"], [["", "Meng"], ["", "Bak"]])
    ]);

    /// <summary>The fake API: what it was sent, and what it answers.</summary>
    private sealed class FakeSaves(ScreenAnswerReply answer)
    {
        public List<ScreenSaveRequest> Sent { get; } = [];
        public string? Address { get; private set; }

        /// <summary>Answers one save, keeping what it was sent.</summary>
        public HttpResponseMessage Answer(HttpRequestMessage request)
        {
            Address = request.RequestUri!.AbsolutePath;
            string body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            Sent.Add(JsonSerializer.Deserialize<ScreenSaveRequest>(body, AdminJson.SnakeCase)!);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(answer, options: AdminJson.SnakeCase)
            };
        }
    }

    /// <summary>The form over that API, and how many times it said it saved.</summary>
    private static (IRenderedComponent<ScreenForm> Form, List<int> Saved) Render(BunitContext ctx, FakeSaves api)
    {
        var saved = new List<int>();
        ctx.Services.AddSingleton(new AgentApi(
            new HttpClient(new StubHandler(api.Answer)) { BaseAddress = new Uri("http://localhost/") }, new ChatSettings()));

        var form = ctx.Render<ScreenForm>(p => p
            .Add(c => c.ScreenName, "dishes")
            .Add(c => c.RowId, "row 1")
            .Add(c => c.Record, Form)
            .Add(c => c.OnSaved, EventCallback.Factory.Create(new object(), () => saved.Add(1))));
        return (form, saved);
    }

    private static FakeSaves Accepting() => new(new ScreenAnswerReply(true, "Saved.", new Dictionary<string, string>()));

    /// <summary>Each kind draws as its own input, with what it holds and its label.</summary>
    [Test]
    public void EachKind_DrawsItsInput()
    {
        using var ctx = new BunitContext();

        var (form, _) = Render(ctx, Accepting());

        var inputs = form.FindAll(".screen-field input");
        Assert.That(inputs.Select(input => input.GetAttribute("type")), Is.EqualTo(new[] { "text", "number", "url", "url" }));
        Assert.That(inputs[0].GetAttribute("value"), Is.EqualTo("Melktert"));
        Assert.That(form.FindAll(".screen-field")[0].TextContent, Does.Contain("NAME"));
    }

    /// <summary>A field that's required says so, and one that can't change is locked.</summary>
    [Test]
    public void RequiredAndReadOnly_AreMarked()
    {
        using var ctx = new BunitContext();

        var (form, _) = Render(ctx, Accepting());

        var inputs = form.FindAll(".screen-field input");
        Assert.That(inputs[0].HasAttribute("required"), Is.True);
        Assert.That(inputs[3].HasAttribute("readonly"), Is.True);
    }

    /// <summary>What was typed goes to the row's address; a read-only field is never sent.</summary>
    [Test]
    public void Saving_SendsWhatWasTyped_NeverAReadOnlyField()
    {
        using var ctx = new BunitContext();
        var api = Accepting();
        var (form, _) = Render(ctx, api);

        form.FindAll(".screen-field input")[0].Change("Melktert 2");
        form.Find(".screen-form").Submit();

        form.WaitForAssertion(() => Assert.That(api.Sent, Has.Count.EqualTo(1)), TimeSpan.FromSeconds(10));
        Assert.That(api.Address, Is.EqualTo("/v1/admin/screens/dishes/rows/row%201"));
        Assert.That(api.Sent[0].Values!["name"], Is.EqualTo("Melktert 2"));
        Assert.That(api.Sent[0].Values!["minutes"], Is.EqualTo("45"));
        Assert.That(api.Sent[0].Values!.ContainsKey("page"), Is.False);
    }

    /// <summary>The rows go too, in the order shown.</summary>
    [Test]
    public void Saving_SendsTheRowsInOrder()
    {
        using var ctx = new BunitContext();
        var api = Accepting();
        var (form, _) = Render(ctx, api);

        form.Find(".screen-rows .screen-row-down").Click();
        form.Find(".screen-form").Submit();

        form.WaitForAssertion(() => Assert.That(api.Sent, Has.Count.EqualTo(1)), TimeSpan.FromSeconds(10));
        Assert.That(api.Sent[0].Rows!["steps"].Select(row => row[1]), Is.EqualTo(new[] { "Bak", "Meng" }));
    }

    /// <summary>A save that worked says so, and tells the page to read the row again.</summary>
    [Test]
    public void ASaveThatWorked_SaysSo_AndTellsThePage()
    {
        using var ctx = new BunitContext();
        var (form, saved) = Render(ctx, Accepting());

        form.Find(".screen-form").Submit();

        form.WaitForAssertion(() => Assert.That(saved, Has.Count.EqualTo(1)), TimeSpan.FromSeconds(10));
        Assert.That(form.Find(".screen-answer").TextContent, Is.EqualTo("Saved."));
    }

    /// <summary>A refusal shows beside its field, and the page isn't told it saved.</summary>
    [Test]
    public void ARefusal_ShowsBesideItsField()
    {
        using var ctx = new BunitContext();
        var refused = new ScreenAnswerReply(false, "Not saved.", new Dictionary<string, string> { ["name"] = "A name is required." });
        var (form, saved) = Render(ctx, new FakeSaves(refused));

        form.Find(".screen-form").Submit();

        form.WaitForAssertion(() => Assert.That(form.FindAll(".screen-field-error"), Has.Count.EqualTo(1)),
            TimeSpan.FromSeconds(10));
        Assert.That(form.FindAll(".screen-field")[0].TextContent, Does.Contain("A name is required."));
        Assert.That(form.Find(".screen-answer").TextContent, Is.EqualTo("Not saved."));
        Assert.That(saved, Is.Empty);
    }
}
