using System.Net;
using System.Text;
using System.Text.Json;
using LouisAgent.Testing;
using LouisAgent.Web.Evals.Admin.Reviews;
using LouisAgent.Web.Evals.Pages.Building;
using LouisAgent.Web.Api;
using LouisAgent.Web.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Evals.Tests.Components.Studio;

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
        """{"id":"d1","identity":"weather-agent","name":"weather-eval","json":"{\"name\":\"weather-eval\",\"identity\":\"weather-agent\",\"version\":3,\"proves\":\"Weather\",\"modes\":[],\"checks\":[{\"type\":\"amount-under\",\"parameters\":{\"key\":\"budget\"}}]}","rubrics":[{"name":"weather-right.md","text":"Is die weer reg?"}],"author":"Louis","at":"2026-11-03T10:00:00Z","scenarios":[{"name":"test-1.json","text":"{}"}]}""";

    /// <summary>
    /// The Evals page's reply: one eval with a recorded run, which a try can replay, and one that recorded nothing.
    /// </summary>
    private const string Evals =
        """{"evals":[{"name":"weather-eval","identity":"weather-agent","version":3,"proves":"Weather","manual":false,"categories":[],"graders":[],"modes":[],"models":[],"never_run":false,"runs":[{"run":{"id":"c0362ff73151","check":"weather-eval","repeat":1,"status":"completed","started_at":"2026-11-04T09:00:00Z","recorded":true},"modes":[]},{"run":{"id":"live01","check":"weather-eval","repeat":1,"status":"completed","started_at":"2026-11-04T10:00:00Z","recorded":false},"modes":[]}]}],"store":{"configured":true}}""";

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
            ("GET", "/v1/admin/evals") => Json(Evals),
            ("PUT", "/v1/admin/evals/drafts/d1") => Json(Draft),
            ("POST", "/v1/admin/evals/drafts/d1/publish") => Json(
                """{"branch":"eval/weather-agent-weather-eval","commit":"abc123","folder":"identities/louis-agent.identity.weather-agent/Evals/WeatherEval"}"""),
            ("POST", "/v1/admin/evals/drafts/d1/try") =>
                Json("""{"id":"run_9","check":"weather-eval","repeat":1,"status":"running"}"""),
            ("POST", "/v1/admin/evals/drafts/d1/verdict") =>
                Json("""{"ok":false,"problems":["scenario weather-paarl has no reference transcript"]}"""),
            ("POST", "/v1/admin/evals/drafts/promptfoo") =>
                Json($$"""{"draft":{{Draft}},"left_out":["promptfoo's 'javascript' assert has no check of ours"]}"""),
            ("GET", "/v1/admin/evals/drafts/d1/promptfoo") => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("description: Weather\n", Encoding.UTF8, "application/yaml")
            },
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

    /// <summary>
    /// The form is four numbered steps in the order they are done: compose, check, try, publish (the admin review's
    /// finding 6: the order on the page, not in the person's head).
    /// </summary>
    [Test]
    public void TheForm_IsFourStepsInOrder()
    {
        var (cut, _) = Render();

        cut.Find(".studio-draft").Click();

        Assert.That(cut.FindAll(".studio-step > h4").Select(h => h.TextContent.Trim()),
            Is.EqualTo(new[] { "1 Compose", "2 Check", "3 Try", "4 Publish" }));
    }

    /// <summary>
    /// Try offers only the recorded runs to replay, picked from a list instead of an id typed from memory.
    /// </summary>
    [Test]
    public void Try_OffersTheRecordedRuns_ToPickFrom()
    {
        var (cut, _) = Render();

        cut.Find(".studio-draft").Click();

        cut.WaitForAssertion(() => Assert.That(
            cut.FindAll(".studio-try-replay option").Select(o => o.GetAttribute("value")),
            Is.EqualTo(new[] { "", "c0362ff73151" })));
    }

    /// <summary>
    /// Try saves the draft, then starts a replay of the run picked on the samples typed, and links to its run.
    /// </summary>
    [Test]
    public void Try_SavesTheDraft_ThenStartsAReplay_OfTheRunPicked()
    {
        var (cut, handler) = Render();
        cut.Find(".studio-draft").Click();
        cut.WaitForAssertion(() => Assert.That(cut.FindAll(".studio-try-replay option"), Has.Count.EqualTo(2)));
        cut.Find(".studio-try-replay").Change("c0362ff73151");
        cut.Find(".studio-try-samples").Change("weather-paarl, weather-af");

        cut.Find(".studio-try-start").Click();

        cut.WaitForAssertion(() => Assert.That(cut.Find(".studio-tried").GetAttribute("href"),
            Is.EqualTo("admin/runs/run_9")));
        var sent = JsonDocument.Parse(handler.Requests.Single(r => r.Url.EndsWith("/try")).Body!).RootElement;
        Assert.That((sent.GetProperty("replay").GetString(), sent.GetProperty("samples").GetArrayLength()),
            Is.EqualTo(("c0362ff73151", 2)));
    }

    /// <summary>Publish saves the draft, then names the branch it was committed on, for the person to push.</summary>
    [Test]
    public void Publish_SavesTheDraft_ThenNamesTheBranch()
    {
        var (cut, handler) = Render();
        cut.Find(".studio-draft").Click();

        cut.Find(".studio-publish").Click();

        cut.WaitForAssertion(() => Assert.That(cut.Find(".studio-published").TextContent,
            Does.Contain("eval/weather-agent-weather-eval").And.Contain("pull request")));
        Assert.That(handler.Requests.Select(r => r.Method.Method), Does.Contain("PUT"), "saved first");
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

    /// <summary>A draft's own scenario files, as an import brought them, go back with it when it is saved.</summary>
    [Test]
    public void Save_KeepsTheDraftsOwnScenarios()
    {
        var (cut, handler) = Render();
        cut.Find(".studio-draft").Click();

        cut.Find(".studio-save").Click();

        cut.WaitForAssertion(() => Assert.That(handler.Requests.Any(r => r.Method == HttpMethod.Put), Is.True));
        var sent = JsonDocument.Parse(handler.Requests.Single(r => r.Method == HttpMethod.Put).Body!).RootElement;
        Assert.That(sent.GetProperty("scenarios")[0].GetProperty("name").GetString(), Is.EqualTo("test-1.json"));
    }

    /// <summary>An import sends the pasted file, opens the draft it made and names what was left out.</summary>
    [Test]
    public void APromptfooImport_OpensTheDraft_AndNamesWhatWasLeftOut()
    {
        var (cut, handler) = Render();

        cut.Find(".promptfoo-yaml").Change("description: Weather\n");
        cut.Find(".promptfoo-import").Click();

        cut.WaitForAssertion(() => Assert.That(cut.Find(".promptfoo-left-out").TextContent,
            Does.Contain("javascript")));
        Assert.That(cut.Find(".compose-name").GetAttribute("value"), Is.EqualTo("weather-eval"));
        var sent = JsonDocument.Parse(handler.Requests.Single(r => r.Url.EndsWith("/promptfoo")).Body!).RootElement;
        Assert.That((sent.GetProperty("yaml").GetString(), sent.GetProperty("author").GetString()),
            Is.EqualTo(("description: Weather\n", "Louis")));
    }

    /// <summary>An export saves the draft, then fetches its promptfoo file for the browser to save.</summary>
    [Test]
    public void APromptfooExport_SavesTheDraft_ThenFetchesTheFile()
    {
        var (cut, handler) = Render();
        cut.Find(".studio-draft").Click();

        cut.Find(".promptfoo-export").Click();

        cut.WaitForAssertion(() => Assert.That(cut.Find(".promptfoo-exported").TextContent,
            Does.Contain("promptfooconfig.yaml")));
        Assert.That(handler.Requests.Select(r => r.Method.Method + " " + new Uri(r.Url).AbsolutePath),
            Is.SupersetOf(new[] { "PUT /v1/admin/evals/drafts/d1", "GET /v1/admin/evals/drafts/d1/promptfoo" }));
    }
}
