using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LouisAgent.Api.Evals.Reviews;
using LouisAgent.Api.Evals.Reviews.Store;
using LouisAgent.Api.Evals.Runs.Kinds;
using LouisAgent.Api.Evals.Store;
using LouisAgent.Testing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Protocol.Parts.Conversations;
using static LouisAgent.Api.Evals.Store.EvalDocuments;

namespace LouisAgent.Api.Tests.Endpoints;

/// <summary>
/// Pins the JSON of the people's grades: the run's list byte for byte, and a new grade's names (its time is the
/// server's clock), so the review panel reads them however the routes are shaped.
/// </summary>
public class ReviewReplyJsonTests
{
    /// <summary>The admin key the test host is built with, and that each request sends.</summary>
    private const string AdminKey = "admin-secret";

    /// <summary>A grade's field names, in the order the review panel reads them.</summary>
    private const string GradeNames = "mode,repeat,task,grader,model_grader,verdict,note,reviewer,at,latest";

    /// <summary>When the run started and the stored grade was given.</summary>
    private static readonly DateTimeOffset At = new(2026, 10, 25, 9, 0, 0, TimeSpan.Zero);

    /// <summary>The one conversation the run holds.</summary>
    private static readonly ConversationKey Key = new(SampleMode.Beta, 1, "bobotie");

    /// <summary>The list carries each grade with whether it is its reviewer's latest.</summary>
    [Test]
    public async Task TheList_IsWrittenAsBefore()
    {
        var (store, reviews) = await StoredAsync();
        await reviews.AddAsync(new HumanGrade("run_1", Key, "meal", HumanVerdict.Pass, "Fine.", "louis", At));
        await using var app = await StartAsync(store, reviews);

        using var response = await SendAsync(app, HttpMethod.Get, "/v1/admin/benchmarks/runs/run_1/reviews");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await response.Content.ReadAsStringAsync(), Is.EqualTo(
            """{"reviews":[{"mode":"mode-beta","repeat":1,"task":"bobotie","grader":"meal."""
            + """human","model_grader":"meal","verdict":"pass","note":"Fine.","reviewer":"lou"""
            + """is","at":"2026-10-25T09:00:00+00:00","latest":true}]}"""));
    }

    /// <summary>A new grade comes back as the grade kept, with the same names.</summary>
    [Test]
    public async Task ANewGrade_KeepsItsNames()
    {
        var (store, reviews) = await StoredAsync();
        await using var app = await StartAsync(store, reviews);

        using var response = await SendAsync(app, HttpMethod.Put,
            $"/v1/admin/benchmarks/runs/run_1/reviews/{SampleMode.Beta}/1/bobotie/meal.human",
            new
            {
                verdict = "pass",
                note = "Fine.",
                reviewer = "louis"
            });
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(string.Join(",", json.RootElement.EnumerateObject().Select(p => p.Name)), Is.EqualTo(GradeNames));
    }

    /// <summary>A run with a human review of the meal judge, and one conversation to grade.</summary>
    private static async Task<(InMemoryEvalStore Store, InMemoryHumanReviewStore Reviews)> StoredAsync()
    {
        var store = new InMemoryEvalStore();
        await store.SaveRunAsync(new Run("run_1", SampleEval.Beta, 1, EvalRunStatus.Done, At,
            Graders: [new Grader("meal.human", 1, GraderKind.Human, "Is this a meal?")]));
        await store.SaveConversationAsync("run_1", new Conversation(Key, 1, null, null, 0, 1));

        return (store, new InMemoryHumanReviewStore());
    }

    /// <summary>Starts the API with the admin key over the stores.</summary>
    private static async Task<WebApplication> StartAsync(IEvalStore store, IHumanReviewStore reviews)
    {
        var app = ApiTestHost.Build(adminKey: AdminKey, store: store, reviews: reviews);
        await app.StartAsync();

        return app;
    }

    /// <summary>Sends a request with the admin key, and a JSON body if one is given.</summary>
    private static Task<HttpResponseMessage> SendAsync(WebApplication app, HttpMethod method, string path,
        object? body = null)
    {
        var request = new HttpRequestMessage(method, path);
        if (body is not null) request.Content = JsonContent.Create(body);
        request.Headers.Add("x-api-key", AdminKey);

        return app.GetTestClient().SendAsync(request);
    }
}
