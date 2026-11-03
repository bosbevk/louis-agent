using System.Net;
using System.Text;
using System.Text.Json;
using LouisAgent.Testing;
using LouisAgent.Puls.Admin.Reviews;
using LouisAgent.Puls.Components.Admin.Studio;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Puls.Tests.Components.Admin.Studio;

/// <summary>
/// Under a graded review, the keep buttons: a pass offers "keep as reference", a fail a reason and "keep as
/// known-bad", an unsure nothing. Keeping posts a transcript draft with the person's name and shows the file it will
/// be.
/// </summary>
public class KeepButtonsTests
{
    /// <summary>The conversation that was graded.</summary>
    private static readonly ConversationRef CookAf = new("run1", SampleMode.Beta, 1, "cook-af");

    /// <summary>Renders the buttons for this verdict over an API that keeps any transcript as this file.</summary>
    private static (IRenderedComponent<KeepButtons> Cut, StubHandler Handler) Render(string? verdict,
        string file = "cook-af.reference.txt")
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.JSInterop.Setup<string?>("localStorage.getItem", ReviewerState.StorageKey).SetResult("Louis");
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent(
                $$"""{"id":"t1","eval":"cook","identity":"puls","run":"run1","conversation":{"mode":"beta","repeat":1,"task":"cook-af"},"kind":"reference","reason":null,"file_name":"{{file}}","lines":3,"text":"","author":"Louis","at":"2026-11-03T10:00:00Z"}""",
                Encoding.UTF8, "application/json")
        });
        ctx.Services.AddSingleton(new AgentApi(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") },
            new ChatSettings()));
        var state = new ReviewerState(ctx.JSInterop.JSRuntime);
        ctx.Services.AddSingleton(state);
        var cut = ctx.Render<KeepButtons>(p => p.Add(x => x.Conversation, CookAf).Add(x => x.Verdict, verdict));
        cut.WaitForState(() => state.Name == "Louis");
        return (cut, handler);
    }

    /// <summary>A pass offers a reference, a fail a known-bad with its reason, an unsure nothing.</summary>
    [Test]
    public void EachVerdict_OffersItsKeep()
    {
        Assert.That(Render("pass").Cut.FindAll(".keep-reference"), Has.Count.EqualTo(1));
        Assert.That(Render("fail").Cut.FindAll(".keep-bad, .keep-reason"), Has.Count.EqualTo(2));
        Assert.That(Render("unsure").Cut.FindAll("button"), Is.Empty);
    }

    /// <summary>Keeping a pass posts a reference with the person's name, and shows its file.</summary>
    [Test]
    public void KeepingAPass_PostsAReference_AndShowsItsFile()
    {
        var (cut, handler) = Render("pass");

        cut.Find(".keep-reference").Click();

        cut.WaitForAssertion(() =>
            Assert.That(cut.Find(".keep-done").TextContent, Does.Contain("cook-af.reference.txt")));
        var (method, url, body) = handler.Requests.Single();
        Assert.That((method, url),
            Is.EqualTo((HttpMethod.Post, "http://localhost/v1/admin/benchmarks/runs/run1/transcripts")));
        var sent = JsonDocument.Parse(body!).RootElement;
        Assert.That((sent.GetProperty("kind").GetString(), sent.GetProperty("author").GetString()),
            Is.EqualTo(("reference", "Louis")));
    }

    /// <summary>Keeping a fail posts a known-bad with the reason typed.</summary>
    [Test]
    public void KeepingAFail_PostsAKnownBad_WithItsReason()
    {
        var (cut, handler) = Render("fail", "cook-af.bad.no-shop.txt");
        cut.Find(".keep-reason").Change("no-shop");

        cut.Find(".keep-bad").Click();

        cut.WaitForAssertion(() => Assert.That(handler.Requests, Has.Count.EqualTo(1)));
        var sent = JsonDocument.Parse(handler.Requests[0].Body!).RootElement;
        Assert.That((sent.GetProperty("kind").GetString(), sent.GetProperty("reason").GetString()),
            Is.EqualTo(("bad", "no-shop")));
    }
}
