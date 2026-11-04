using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LouisAgent.Api.Evals.Drafts.Store;
using LouisAgent.Api.Evals.List;
using LouisAgent.Api.Evals.Reviews;
using LouisAgent.Api.Evals.Reviews.Store;
using LouisAgent.Api.Evals.Runs.Kinds;
using LouisAgent.Api.Evals.Store;
using LouisAgent.Evals.Protocol.Parts.Conversations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using static LouisAgent.Api.Evals.Store.EvalDocuments;

namespace LouisAgent.Api.Tests.Evals.Drafts;

/// <summary>
/// The studio's draft routes: a stored run's conversation becomes a sample draft, a reviewed one a reference or
/// known-bad transcript draft, and an eval's drafts are listed and deleted. A run, conversation, scenario, eval or
/// draft that isn't there is a 404; a bad id, reason, expected key or a kind the verdicts don't back is a 400.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0039-api-routes-under-v1.md">ADR 0039: API routes under v1</seealso>
public class DraftEndpointTests
{
    private const string AdminKey = "admin-secret";
    private const string RunId = "8f7802b9a72e";
    private const string Cook = "configured/puls/evals/louis-agent.identity.puls.evals/Evals/Cook";
    private static readonly DateTimeOffset T0 = new(2026, 11, 3, 9, 0, 0, TimeSpan.Zero);

    /// <summary>The running API with its client and the draft store, so a test can check what was kept.</summary>
    private sealed record Host(WebApplication App, HttpClient Client, InMemoryEvalDraftStore Drafts)
        : IAsyncDisposable
    {
        /// <summary>Stops the API when the test is done.</summary>
        public async ValueTask DisposeAsync() => await App.DisposeAsync();
    }

    /// <summary>
    /// Starts the API over one run of the cook eval: cook-af in two turns that a person passed, cook-paarl in one that
    /// a person failed, and cook-atlantis, a scenario the eval's folder doesn't have, that a person was unsure of.
    /// </summary>
    private static async Task<Host> StartAsync()
    {
        // The run, in one mode.
        var store = new InMemoryEvalStore();
        await store.SaveRunAsync(new Run(RunId, "cook", 1, EvalRunStatus.Done, T0, T0.AddMinutes(5), 0,
            Modes:
            [
                new Mode(SampleMode.Beta, 1, [SampleToolset.Beta], "puls/voice-af", null, Conversations: 3, At: T0)
            ]));

        // Each conversation with its turns, each turn with one stream event, and a person's verdict on it.
        var reviews = new InMemoryHumanReviewStore();
        foreach (var (task, verdict, questions) in new[]
                 {
                     ("cook-af", HumanVerdict.Pass, new[] { "Wat kan ek kook?", "Vir ses?" }),
                     ("cook-paarl", HumanVerdict.Fail, new[] { "Iets in Paarl?" }),
                     ("cook-atlantis", HumanVerdict.Unsure, new[] { "Iets in Atlantis?" })
                 })
        {
            var key = new ConversationKey(SampleMode.Beta, 1, task);
            await store.SaveConversationAsync(RunId, new Conversation(key, 1, null, null, 0, 1.0));
            for (int turn = 1; turn <= questions.Length; turn++)
                await store.SaveTurnAsync(RunId,
                    new Turn(new TurnKey(SampleMode.Beta, 1, task, turn), "sess_1", questions[turn - 1], "Antwoord.",
                        SampleRole.Alpha, null, 1, 0, [], 1.0, null, AskedAt: T0, EndedAt: T0),
                    [
                        new StreamEvent("message_stop", JsonDocument.Parse("""{"type":"message_stop"}""").RootElement,
                            T0)
                    ]);
            await reviews.AddAsync(new HumanGrade(RunId, key, "meal", verdict, "", "Louis", T0));
        }

        // The cook eval as the runner lists it, its folder the repo's own.
        var catalog = new EvalList([
            new EvalEntry("cook", "Cooks", null,
                new EvalLine("cook", 1, "Cooks", Identity: SampleIdentity.Alpha, Folder: Cook))
        ]);
        var drafts = new InMemoryEvalDraftStore();
        var app = ApiTestHost.Build(adminKey: AdminKey, store: store, catalog: catalog, reviews: reviews,
            drafts: drafts);
        await app.StartAsync();
        return new Host(app, app.GetTestClient(), drafts);
    }

    /// <summary>Sends a request with the admin key, and any body as JSON.</summary>
    private static Task<HttpResponseMessage> Send(HttpClient client, HttpMethod method, string url,
        object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("x-api-key", AdminKey);
        if (body is not null) request.Content = JsonContent.Create(body);
        return client.SendAsync(request);
    }

    /// <summary>
    /// Asks for a sample from a conversation of the run; each test changes only the part it is about.
    /// </summary>
    private static Task<HttpResponseMessage> PostSample(HttpClient client, string task = "cook-af",
        string id = "cook-af-six", object? expected = null, string run = RunId) =>
        Send(client, HttpMethod.Post, $"/v1/admin/benchmarks/runs/{run}/samples",
            new
            {
                conversation = new
                {
                    mode = SampleMode.Beta,
                    repeat = 1,
                    task
                },
                id,
                expected,
                author = "Louis"
            });

    /// <summary>Asks to keep a conversation of the run as a transcript.</summary>
    private static Task<HttpResponseMessage> PostTranscript(HttpClient client, string task, string kind,
        string? reason = null) =>
        Send(client, HttpMethod.Post, $"/v1/admin/benchmarks/runs/{RunId}/transcripts",
            new
            {
                conversation = new
                {
                    mode = SampleMode.Beta,
                    repeat = 1,
                    task
                },
                kind,
                reason,
                author = "Louis"
            });

    /// <summary>Reads an answer's body as JSON.</summary>
    private static async Task<JsonElement> JsonOf(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();

    /// <summary>
    /// A sample is made from the conversation and its scenario: 201 with its row, the scenario file it will be as
    /// JSON, and it is kept for the eval.
    /// </summary>
    [Test]
    public async Task ASample_IsMadeFromTheConversation_AndKeptForTheEval()
    {
        await using var host = await StartAsync();

        var response = await PostSample(host.Client, expected: new { serves = new[] { 6, 6 } });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created),
            await response.Content.ReadAsStringAsync());
        var row = await JsonOf(response);
        Assert.That((row.GetProperty("sample_id").GetString(), row.GetProperty("question").GetString(),
                row.GetProperty("eval").GetString(), row.GetProperty("identity").GetString()),
            Is.EqualTo(("cook-af-six", "Wat kan ek kook?", "cook", SampleIdentity.Alpha)));
        var file = JsonDocument.Parse(row.GetProperty("json").GetString()!).RootElement;
        Assert.That(file.GetProperty("metadata").GetProperty("follow_ups")[0].GetString(), Is.EqualTo("Vir ses?"));
        Assert.That(file.GetProperty("metadata").GetProperty("expected").GetProperty("budget").GetInt32(),
            Is.EqualTo(250), "the scenario's other values stay");
        Assert.That((await host.Drafts.OfEvalAsync("cook")).Samples.Single().Sample.Id, Is.EqualTo("cook-af-six"));
    }

    /// <summary>A sample id that isn't kebab-case, or an expected key the scenario doesn't have, is a 400.</summary>
    [Test]
    public async Task ABadIdOrAnUnknownExpectedKey_IsA400()
    {
        await using var host = await StartAsync();

        var badId = await PostSample(host.Client, id: "Cook AF");
        var badKey = await PostSample(host.Client, expected: new { servings = 6 });

        Assert.That((badId.StatusCode, badKey.StatusCode),
            Is.EqualTo((HttpStatusCode.BadRequest, HttpStatusCode.BadRequest)));
        Assert.That((await JsonOf(badKey)).GetProperty("error").GetProperty("message").GetString(),
            Does.Contain("servings"));
    }

    /// <summary>A run, a conversation or a scenario that isn't there is a 404.</summary>
    [Test]
    public async Task ARunConversationOrScenarioThatIsNotThere_IsA404()
    {
        await using var host = await StartAsync();

        var noRun = await PostSample(host.Client, run: "nope");
        var noConversation = await PostSample(host.Client, task: "cook-mars");
        var noScenario = await PostSample(host.Client, task: "cook-atlantis");

        Assert.That((noRun.StatusCode, noConversation.StatusCode, noScenario.StatusCode),
            Is.EqualTo((HttpStatusCode.NotFound, HttpStatusCode.NotFound, HttpStatusCode.NotFound)));
    }

    /// <summary>
    /// A passed conversation is kept as a reference: its mode line, then each turn's question, stream and turn line,
    /// under the scenario's reference name.
    /// </summary>
    [Test]
    public async Task APassedConversation_IsKeptAsAReference_WithItsProtocolLines()
    {
        await using var host = await StartAsync();

        var response = await PostTranscript(host.Client, "cook-af", "reference");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created),
            await response.Content.ReadAsStringAsync());
        var row = await JsonOf(response);
        Assert.That((row.GetProperty("file_name").GetString(), row.GetProperty("lines").GetInt32()),
            Is.EqualTo(("cook-af.reference.txt", 7)));
        var events = (await host.Drafts.OfEvalAsync("cook")).Transcripts.Single().Lines
            .Select(line => JsonDocument.Parse(line).RootElement.GetProperty("event").GetString());
        Assert.That(events, Is.EqualTo(new[] { "mode", "question", "sse", "turn", "question", "sse", "turn" }));
    }

    /// <summary>A failed conversation is kept as a known-bad with its reason.</summary>
    [Test]
    public async Task AFailedConversation_IsKeptAsAKnownBad() =>
        Assert.That((await JsonOf(await PostTranscript((await StartAsync()).Client, "cook-paarl", "bad", "no-shop")))
            .GetProperty("file_name").GetString(), Is.EqualTo("cook-paarl.bad.no-shop.txt"));

    /// <summary>
    /// A reference of a failed conversation, a known-bad of an unsure one, an unknown kind or a reason that isn't
    /// kebab-case is a 400.
    /// </summary>
    [TestCase("cook-paarl", "reference", null)]
    [TestCase("cook-atlantis", "bad", "no-shop")]
    [TestCase("cook-af", "great", null)]
    [TestCase("cook-paarl", "bad", "No shop")]
    public async Task AKindTheVerdictsDontBack_IsA400(string task, string kind, string? reason)
    {
        await using var host = await StartAsync();

        var response = await PostTranscript(host.Client, task, kind, reason);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    /// <summary>The eval's drafts are listed; an eval the catalogue doesn't have is a 404.</summary>
    [Test]
    public async Task TheEvalsDrafts_AreListed()
    {
        await using var host = await StartAsync();
        await PostSample(host.Client);
        await PostTranscript(host.Client, "cook-af", "reference");

        var list = await JsonOf(await Send(host.Client, HttpMethod.Get, "/v1/admin/evals/cook/drafts"));
        var unknown = await Send(host.Client, HttpMethod.Get, "/v1/admin/evals/bake/drafts");

        Assert.That((list.GetProperty("samples").GetArrayLength(), list.GetProperty("transcripts").GetArrayLength()),
            Is.EqualTo((1, 1)));
        Assert.That(unknown.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>A draft is deleted under its own eval only; a second delete, or another eval's, is a 404.</summary>
    [Test]
    public async Task ADraft_IsDeletedUnderItsOwnEval()
    {
        await using var host = await StartAsync();
        string id = (await JsonOf(await PostSample(host.Client))).GetProperty("id").GetString()!;

        var elsewhere = await Send(host.Client, HttpMethod.Delete, $"/v1/admin/evals/bake/drafts/{id}");
        var deleted = await Send(host.Client, HttpMethod.Delete, $"/v1/admin/evals/cook/drafts/{id}");
        var again = await Send(host.Client, HttpMethod.Delete, $"/v1/admin/evals/cook/drafts/{id}");

        Assert.That((elsewhere.StatusCode, deleted.StatusCode, again.StatusCode),
            Is.EqualTo((HttpStatusCode.NotFound, HttpStatusCode.NoContent, HttpStatusCode.NotFound)));
    }

    /// <summary>The dialog starts from the conversation's scenario: its place and expected values.</summary>
    [Test]
    public async Task TheScenario_IsServedForTheDialog()
    {
        await using var host = await StartAsync();

        var row = await JsonOf(await Send(host.Client, HttpMethod.Get,
            $"/v1/admin/benchmarks/runs/{RunId}/scenarios/cook-af"));

        Assert.That((row.GetProperty("place").GetString(), row.GetProperty("expected").GetProperty("shop").GetString()),
            Is.EqualTo(("Stellenbosch, Wes-Kaap, Suid-Afrika", "pnp")));
    }
}
