using System.Net;
using System.Text;
using LouisAgent.Puls.Components.Admin.Reviews;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Puls.Tests.Components.Admin.Reviews;

// F18 step 6a.5c (Louis: "we needed a gui for calibration in admin"): in a judge's agreement card, a Check the judge button starts the API's calibration of the judge against the
// answers you graded, follows the run until it ends, then fetches the reports again (ADR 0042) and shows the latest in plain sentences with its disagreements, the earlier checks folded.
// Written first: the card did not exist.
public class CalibrationCardTests
{
    private const string None = """{"calibrations":[]}""";

    private const string Two = """
        {"calibrations":[
          {"run":"run1","grader":"meal-for-people","judge_model":"qwen3:4b","at":"2026-10-26T14:02:00+00:00","agreed":35,"total":36,"bar":90,"passes":false,
           "person_pass":{"judge_pass":27,"judge_fail":0,"judge_unknown":0},"person_fail":{"judge_pass":1,"judge_fail":8,"judge_unknown":0},
           "disagreements":[{"scenario":"cook-shops","mode":"puls-cook","repeat":2,"label":"fail","judge":"pass","reason":"a dish with steps"}]},
          {"run":"run1","grader":"meal-for-people","judge_model":"qwen3:4b","at":"2026-10-26T13:00:00+00:00","agreed":32,"total":36,"bar":90,"passes":false,
           "person_pass":{"judge_pass":23,"judge_fail":4,"judge_unknown":0},"person_fail":{"judge_pass":0,"judge_fail":9,"judge_unknown":0},"disagreements":[]}
        ]}
        """;

    private const string Started = """{"id":"cal1","check":"puls-cook","status":"running","calibration_of":"run1","grader":"meal-for-people","last_line":null}""";

    private static HttpResponseMessage Json(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static (IRenderedComponent<CalibrationCard> Cut, StubHandler Handler) Render(string calibrations, string current = """{"id":"cal1","check":"puls-cook","status":"done","calibration_of":"run1","grader":"meal-for-people","last_line":"Result: pass"}""",
        bool hasLabels = true)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var handler = new StubHandler(request => request.RequestUri!.AbsolutePath switch
        {
            var p when p.EndsWith("/runs/current") => Json(current),
            var p when request.Method == HttpMethod.Post && p.EndsWith("/calibrations/meal-for-people") => Json(Started),
            _ => Json(calibrations),
        });
        ctx.Services.AddSingleton(new AgentApi(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }, new ChatSettings()));
        var cut = ctx.Render<CalibrationCard>(p => p.Add(x => x.RunId, "run1").Add(x => x.Grader, "meal-for-people").Add(x => x.HasLabels, hasLabels).Add(x => x.Poll, TimeSpan.Zero));
        return (cut, handler);
    }

    [Test]
    public void TheLatestReport_ReadsAsSentences_WithItsDisagreements_AndTheEarlierChecksFolded()
    {
        var (cut, _) = Render(Two);

        cut.WaitForAssertion(() => Assert.That(cut.FindAll(".calibration-report"), Has.Count.EqualTo(1)));
        Assert.That(cut.Find(".calibration-headline").TextContent, Does.Contain("with qwen3:4b").And.Contain("35 of 36 answers"));
        Assert.That(cut.Find(".calibration-verdict").TextContent, Is.EqualTo("Not yet: of the 9 answers you failed, it agreed on 8, under 90%."));
        Assert.That(cut.Find(".calibration-verdict").ClassList, Does.Contain("short"));
        Assert.That(cut.Find(".calibration-pass .agreement-sentence").TextContent, Is.EqualTo("You passed 27 answers. The judge passed all 27."));
        Assert.That(cut.Find(".calibration-fail .agreement-sentence").TextContent, Is.EqualTo("You failed 9 answers. The judge failed 8 of them."));
        Assert.That(cut.Find(".calibration-disagreements").TextContent, Does.Contain("cook-shops · puls-cook · repeat 2: you said fail, the judge said pass.").And.Contain("a dish with steps"));
        Assert.That(cut.FindAll("details summary").Select(s => s.TextContent), Is.EqualTo(new[] { "Where the judge differed (1)", "Earlier checks (1)" }));
        Assert.That(cut.Find(".calibration-earlier").TextContent, Does.Contain("32 of 36 · not yet"));
    }

    [Test]
    public void NotCheckedYet_SaysSo_AndTheButtonIsReady()
    {
        var (cut, _) = Render(None);

        cut.WaitForAssertion(() => Assert.That(cut.Find(".calibration-none").TextContent, Does.Contain("Not checked yet")));
        Assert.That(cut.Find(".calibration-check").HasAttribute("disabled"), Is.False);
        Assert.That(cut.FindAll(".calibration-report"), Is.Empty);
    }

    [Test]
    public void WithoutLabels_TheButtonWaits_AndSaysWhy()
    {
        var (cut, _) = Render(None, hasLabels: false);

        cut.WaitForAssertion(() => Assert.That(cut.Find(".calibration-none").TextContent, Does.Contain("Grade some answers first")));
        Assert.That(cut.Find(".calibration-check").HasAttribute("disabled"), Is.True);
    }

    [Test]
    public void TheCheck_PostsToTheRun_FollowsItToItsEnd_ThenFetchesTheReportsAgain()
    {
        var (cut, handler) = Render(None);
        cut.WaitForAssertion(() => Assert.That(handler.Requests, Has.Count.EqualTo(1)));

        cut.Find(".calibration-check").Click();

        cut.WaitForAssertion(() => Assert.That(handler.Requests.Select(r => $"{r.Method} {new Uri(r.Url).AbsolutePath}"), Is.EqualTo(new[]
        {
            "GET /v1/admin/benchmarks/runs/run1/calibrations",
            "POST /v1/admin/benchmarks/runs/run1/calibrations/meal-for-people",
            "GET /v1/admin/benchmarks/runs/current",
            "GET /v1/admin/benchmarks/runs/run1/calibrations",
        })));
        // The store had no new report (the stub's list stays empty): the card says so rather than showing nothing.
        cut.WaitForAssertion(() => Assert.That(cut.Find(".review-error").TextContent, Does.Contain("no report was kept")));
    }

    [Test]
    public void AFailedCheck_ShowsTheRunnersLastLine()
    {
        var (cut, _) = Render(None, current: """{"id":"cal1","check":"puls-cook","status":"failed","calibration_of":"run1","grader":"meal-for-people","last_line":"Eval 'puls-cook' has no grader 'x'."}""");
        cut.WaitForAssertion(() => Assert.That(cut.FindAll(".calibration-check"), Has.Count.EqualTo(1)));

        cut.Find(".calibration-check").Click();

        cut.WaitForAssertion(() => Assert.That(cut.Find(".review-error").TextContent, Does.Contain("has no grader 'x'")));
    }

    // 6a.5e (the review's nit): a card that is left while a check runs stops following it; the API's run goes on without a listener.
    [Test]
    public async Task LeavingThePage_StopsTheFollowing()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var handler = new StubHandler(request => request.RequestUri!.AbsolutePath switch
        {
            var p when p.EndsWith("/runs/current") => Json(Started),
            var p when request.Method == HttpMethod.Post && p.EndsWith("/calibrations/meal-for-people") => Json(Started),
            _ => Json(None),
        });
        ctx.Services.AddSingleton(new AgentApi(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }, new ChatSettings()));
        var cut = ctx.Render<CalibrationCard>(p => p.Add(x => x.RunId, "run1").Add(x => x.Grader, "meal-for-people").Add(x => x.HasLabels, true).Add(x => x.Poll, TimeSpan.FromMilliseconds(10)));
        cut.WaitForAssertion(() => Assert.That(cut.FindAll(".calibration-check"), Has.Count.EqualTo(1)));
        cut.Find(".calibration-check").Click();
        // A plain wait for the condition, not a fixed time: under the whole suite's load the card's loop runs later (WaitForAssertion would hold the renderer the loop needs).
        for (int waited = 0; waited < 5000 && handler.Requests.Count(r => r.Url.EndsWith("/runs/current")) <= 3; waited += 20) await Task.Delay(20);
        Assert.That(handler.Requests.Count(r => r.Url.EndsWith("/runs/current")), Is.GreaterThan(3), "the card keeps looking while the check runs");

        ctx.Dispose();
        int atLeaving = handler.Requests.Count;
        await Task.Delay(150);

        Assert.That(handler.Requests.Count, Is.LessThanOrEqualTo(atLeaving + 1), "at most the one look that was in flight");
    }
}
