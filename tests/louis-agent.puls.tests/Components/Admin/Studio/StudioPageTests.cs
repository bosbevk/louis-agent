using System.Net;
using System.Text;
using System.Text.Json;
using LouisAgent.Testing;
using LouisAgent.Puls.Admin.Reviews;
using LouisAgent.Puls.Pages.Admin;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Puls.Tests.Components.Admin.Studio;

/// <summary>
/// The Studio page (F49.5): it lists the evals being composed, opens one into the form, shows the file the form
/// makes, saves it under the reviewer's name, and asks the runner's verdict, showing every problem in its words.
/// </summary>
public class StudioPageTests
{
    /// <summary>The catalogue: one check with a required key.</summary>
    private const string Checks =
        """{"checks":[{"type":"amount-under","kind":"fixed-rule","parameters":{"type":["object","null"],"properties":{"key":{"type":"string"}},"required":["key"]}}],"readers":[]}""";

    /// <summary>One eval being composed.</summary>
    private const string Draft =
        """{"id":"d1","identity":"weather-agent","name":"weather-eval","json":"{\"name\":\"weather-eval\",\"identity\":\"weather-agent\",\"version\":3,\"proves\":\"Weather\",\"modes\":[],\"checks\":[{\"type\":\"amount-under\",\"parameters\":{\"key\":\"budget\"}}]}","rubrics":[{"name":"weather-right.md","text":"Is die weer reg?"}],"author":"Louis","at":"2026-11-03T10:00:00Z"}""";

    /// <summary>Renders the page over a stand-in API, with the reviewer's name kept.</summary>
    private static (IRenderedComponent<EvalStudio> Cut, StubHandler Handler) Render()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.JSInterop.Setup<string?>("localStorage.getItem", ReviewerState.StorageKey).SetResult("Louis");
        var handler = new StubHandler(request => (request.Method.Method, request.RequestUri!.AbsolutePath) switch
        {
            ("GET", "/v1/admin/evals/checks") => Json(Checks),
            ("GET", "/v1/admin/evals/drafts") => Json($$"""{"drafts":[{{Draft}}]}"""),
            ("PUT", "/v1/admin/evals/drafts/d1") => Json(Draft),
            ("POST", "/v1/admin/evals/drafts/d1/try") =>
                Json("""{"id":"run_9","check":"weather-eval","repeat":1,"status":"running"}"""),
            ("POST", "/v1/admin/evals/drafts/d1/verdict") =>
                Json("""{"ok":false,"problems":["scenario weather-paarl has no reference transcript"]}"""),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        });
        ctx.Services.AddSingleton(new AgentApi(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") },
            new ChatSettings()));
        ctx.Services.AddSingleton(new ReviewerState(ctx.JSInterop.JSRuntime));
        ctx.Services.AddSingleton(new ChatStore(ctx.JSInterop.JSRuntime, new ChatSettings()));
        var cut = ctx.Render<EvalStudio>();
        cut.WaitForAssertion(() => Assert.That(cut.FindAll(".studio-draft"), Has.Count.EqualTo(1)));
        return (cut, handler);
    }

    /// <summary>An answer with this JSON body.</summary>
    private static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    /// <summary>A draft opens into the form: its name, version, its check's value and its rubric.</summary>
    [Test]
    public void ADraft_OpensIntoTheForm()
    {
        var (cut, _) = Render();

        cut.Find(".studio-draft").Click();

        Assert.That((cut.Find(".compose-name").GetAttribute("value"),
                cut.Find(".compose-version").GetAttribute("value"),
                cut.Find(".compose-value").GetAttribute("value"),
                cut.Find(".compose-rubric-name").GetAttribute("value")),
            Is.EqualTo(("weather-eval", "3", "budget", "weather-right.md")));
    }

    /// <summary>View JSON shows the file the form makes.</summary>
    [Test]
    public void ViewJson_ShowsTheFileTheFormMakes()
    {
        var (cut, _) = Render();
        cut.Find(".studio-draft").Click();

        cut.Find(".studio-view-json").Click();

        var file = JsonDocument.Parse(cut.Find(".studio-json").TextContent).RootElement;
        Assert.That((file.GetProperty("name").GetString(), file.GetProperty("version").GetInt32(),
                file.GetProperty("checks")[0].GetProperty("parameters").GetProperty("key").GetString()),
            Is.EqualTo(("weather-eval", 3, "budget")));
    }

    /// <summary>Try saves the draft, then starts a replay on the samples typed, and links to where it runs.</summary>
    [Test]
    public void Try_SavesTheDraft_ThenStartsAReplay_OnTheSamplesTyped()
    {
        var (cut, handler) = Render();
        cut.Find(".studio-draft").Click();
        cut.Find(".studio-try-replay").Change("c0362ff73151");
        cut.Find(".studio-try-samples").Change("weather-paarl, weather-af");

        cut.Find(".studio-try-start").Click();

        cut.WaitForAssertion(() => Assert.That(cut.Find(".studio-tried").TextContent, Does.Contain("run_9")));
        var sent = JsonDocument.Parse(handler.Requests.Single(r => r.Url.EndsWith("/try")).Body!).RootElement;
        Assert.That((sent.GetProperty("replay").GetString(), sent.GetProperty("samples").GetArrayLength()),
            Is.EqualTo(("c0362ff73151", 2)));
    }

    /// <summary>Check saves the draft under the reviewer's name, then shows the runner's problems.</summary>
    [Test]
    public void Check_SavesTheDraft_ThenShowsTheRunnersProblems()
    {
        var (cut, handler) = Render();
        cut.Find(".studio-draft").Click();

        cut.Find(".studio-check").Click();

        cut.WaitForAssertion(() => Assert.That(cut.Find(".studio-problems").TextContent,
            Does.Contain("no reference transcript")));
        var put = handler.Requests.Single(r => r.Method == HttpMethod.Put);
        var sent = JsonDocument.Parse(put.Body!).RootElement;
        Assert.That((sent.GetProperty("author").GetString(), sent.GetProperty("rubrics")[0].GetProperty("name")
            .GetString()), Is.EqualTo(("Louis", "weather-right.md")));
        Assert.That(JsonDocument.Parse(sent.GetProperty("json").GetString()!).RootElement.GetProperty("name")
            .GetString(), Is.EqualTo("weather-eval"));
    }
}
