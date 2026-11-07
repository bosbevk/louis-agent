using System.Net;
using System.Text;
using System.Text.Json;
using LouisAgent.Testing;
using LouisAgent.Web.Admin.Reviews;
using LouisAgent.Web.Components.Admin.Evals.Studio;
using LouisAgent.Web.Api;
using LouisAgent.Web.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Admin.Evals.Studio;

/// <summary>
/// The sample dialog on a conversation: it reads the scenario the conversation ran and starts the form from it, sends
/// what the person typed as a sample draft with their name, and says what it kept. Expected values that aren't a JSON
/// object never leave the page; the API's refusal is shown in its own words.
/// </summary>
public class SampleDialogTests
{
    /// <summary>The conversation the sample comes from.</summary>
    private static readonly ConversationRef CookAf = new("run1", SampleMode.Beta, 1, "cook-af");

    /// <summary>The scenario the API serves.</summary>
    private const string Scenario =
        """{"id":"cook-af","place":"Stellenbosch","time_zone":"Africa/Johannesburg","expected":{"budget":250},"categories":["language:af"]}""";

    /// <summary>The draft the API answers a sample with.</summary>
    private const string Draft =
        """{"id":"d1","eval":"cook","identity":"puls","run":"run1","conversation":{"mode":"beta","repeat":1,"task":"cook-af"},"sample_id":"cook-af-2","question":"Wat?","json":"{}","author":"Louis","at":"2026-11-03T10:00:00Z"}""";

    /// <summary>The dialog as rendered, the stub API behind it, and whether it was closed.</summary>
    private sealed record Rendered(IRenderedComponent<SampleDialog> Cut, StubHandler Handler, Func<bool> Closed);

    /// <summary>
    /// Renders the dialog for cook-af with a reviewer's name kept, over an API that answers a POST so.
    /// </summary>
    private static Rendered Render(HttpStatusCode post = HttpStatusCode.Created)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.JSInterop.Setup<string?>("localStorage.getItem", ReviewerState.StorageKey).SetResult("Louis");
        var handler = new StubHandler(request => request.Method == HttpMethod.Get
            ? Json(HttpStatusCode.OK, Scenario)
            : Json(post, post == HttpStatusCode.Created
                ? Draft
                : """{"type":"error","error":{"type":"invalid_request_error","message":"'servings' is not an expected key"}}"""));
        ctx.Services.AddSingleton(new AgentApi(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") },
            new ChatSettings()));
        ctx.Services.AddSingleton(new ReviewerState(ctx.JSInterop.JSRuntime));

        bool closed = false;
        var cut = ctx.Render<SampleDialog>(p => p.Add(x => x.Conversation, CookAf)
            .Add(x => x.OnClose, () => closed = true));
        cut.WaitForAssertion(() => Assert.That(cut.Find(".sample-id").GetAttribute("value"), Is.EqualTo("cook-af-2")));
        return new Rendered(cut, handler, () => closed);
    }

    /// <summary>An answer with this status and JSON body.</summary>
    private static HttpResponseMessage Json(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    /// <summary>It asks for the conversation's scenario and starts the form from it.</summary>
    [Test]
    public void ItStartsFromTheScenario()
    {
        var (cut, handler, _) = Render();

        Assert.That(handler.Requests[0].Url, Does.EndWith("v1/admin/benchmarks/runs/run1/scenarios/cook-af"));
        Assert.That(cut.Find(".sample-place").TextContent, Does.Contain("Stellenbosch"));
        Assert.That(cut.Find(".sample-expected").GetAttribute("value"), Does.Contain("250"));
        Assert.That(cut.Find(".sample-categories").GetAttribute("value"), Is.EqualTo("language:af"));
    }

    /// <summary>Making it posts the sample with the person's name, and says what was kept.</summary>
    [Test]
    public void MakingIt_PostsTheSample_AndSaysWhatWasKept()
    {
        var (cut, handler, _) = Render();
        cut.Find(".sample-target").Change("Bobotie vir vier.");

        cut.Find(".sample-make").Click();

        cut.WaitForAssertion(() => Assert.That(cut.Find(".sample-done").TextContent, Does.Contain("cook-af-2")));
        var (method, url, body) = handler.Requests[1];
        Assert.That((method, url),
            Is.EqualTo((HttpMethod.Post, "http://localhost/v1/admin/benchmarks/runs/run1/samples")));
        var sent = JsonDocument.Parse(body!).RootElement;
        Assert.That((sent.GetProperty("id").GetString(), sent.GetProperty("author").GetString(),
                sent.GetProperty("target").GetString(),
                sent.GetProperty("conversation").GetProperty("task").GetString()),
            Is.EqualTo(("cook-af-2", "Louis", "Bobotie vir vier.", "cook-af")));
    }

    /// <summary>Expected values that aren't a JSON object are refused on the page, and nothing is sent.</summary>
    [Test]
    public void ExpectedValuesThatAreNotJson_AreRefusedOnThePage()
    {
        var (cut, handler, _) = Render();
        cut.Find(".sample-expected").Change("{ budget: 300");

        cut.Find(".sample-make").Click();

        Assert.That(cut.Find(".sample-error").TextContent, Does.Contain("JSON object"));
        Assert.That(handler.Requests, Has.Count.EqualTo(1), "only the scenario was read");
    }

    /// <summary>The API's refusal is shown in its own words.</summary>
    [Test]
    public void TheApisRefusal_IsShown() =>
        Assert.That(Made(Render(HttpStatusCode.BadRequest)).Find(".sample-error").TextContent,
            Does.Contain("'servings' is not an expected key"));

    /// <summary>Close tells the card to close the dialog.</summary>
    [Test]
    public void Close_TellsTheCard()
    {
        var rendered = Render();

        rendered.Cut.Find(".sample-close").Click();

        Assert.That(rendered.Closed(), Is.True);
    }

    /// <summary>Clicks make and waits for the answer.</summary>
    private static IRenderedComponent<SampleDialog> Made(Rendered rendered)
    {
        rendered.Cut.Find(".sample-make").Click();
        rendered.Cut.WaitForAssertion(() => Assert.That(rendered.Handler.Requests, Has.Count.EqualTo(2)));
        return rendered.Cut;
    }
}
