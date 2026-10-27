using LouisAgent.Testing;
using System.Net;
using System.Text;
using System.Text.Json;
using LouisAgent.Puls.Components.Admin.Summary;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Puls.Tests.Components.Admin.Summary;

// F18 step 5c.3: a run's page shows each mode's roll-up: the overall line first, then its categories, each with n, passed of the counted, the pass rate, pass@k and pass^k, the Unknown and
// infrastructure counts and the cost per trial and per pass. A slice of too few trials reads "2 of 3 passed · too few for a rate", never a percentage; an unknown cost reads "unknown" and a
// re-score's "$0". A slice's tasks link to their conversations in the run explorer. A run from before 5c says so and offers the re-score. Written first: the panel was empty.
public class SummaryPanelTests
{
    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();

    private static readonly string Run = SampleEval.Fill(SampleMode.Fill("""
        {"run":{"id":"run123456789","check":"<EvalBeta>"},"summaries":[
          {"mode":"<ModeBeta>","slice":"overall","n":8,"passed":6,"failed":1,"unknown":1,"infrastructure":0,"tasks":["cook-af","cook-en"],"too_few":false,"pass_rate":0.75,"pass_at_k":1,"pass_hat_k":0.5,"cost_per_trial":0.01,"cost_per_pass":0.0133},
          {"mode":"<ModeBeta>","slice":"language:af","n":3,"passed":2,"failed":1,"unknown":0,"infrastructure":0,"tasks":["cook-af"],"too_few":true,"pass_rate":null,"pass_at_k":null,"pass_hat_k":null,"cost_per_trial":null,"cost_per_pass":0.2},
          {"mode":"<ModeBeta>","slice":"budget","n":2,"passed":0,"failed":2,"unknown":0,"infrastructure":0,"tasks":["cook-malmesbury"],"too_few":true,"pass_rate":null,"pass_at_k":null,"pass_hat_k":null,"cost_per_trial":0},
          {"mode":"<ModeGamma>","slice":"overall","n":8,"passed":2,"failed":5,"unknown":0,"infrastructure":1,"tasks":["cook-af"],"too_few":false,"pass_rate":0.2857,"pass_at_k":0.25,"pass_hat_k":0.25,"cost_per_trial":0.02}]}
        """));

    private static (IRenderedComponent<SummaryPanel> Cut, StubHandler Handler) Render(string run)
    {
        var ctx = new BunitContext();
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent("""{"id":"new987654321"}""", Encoding.UTF8, "application/json"),
        });
        ctx.Services.AddSingleton(new AgentApi(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }, new ChatSettings()));
        return (ctx.Render<SummaryPanel>(p => p.Add(x => x.Run, Json(run))), handler);
    }

    [Test]
    public void EachModeHasItsOwnTable_WithTheOverallLineFirst_ThenItsCategories()
    {
        var (cut, _) = Render(Run);

        var modes = cut.FindAll(".summary-mode");
        Assert.That(modes.Select(m => m.QuerySelector("h3")!.TextContent), Is.EqualTo(new[] { SampleMode.Beta, SampleMode.Gamma }));
        Assert.That(modes[0].QuerySelectorAll(".s-slice").Select(c => c.TextContent), Is.EqualTo(new[] { "overall", "language:af", "budget" }));
    }

    [Test]
    public void ALineShowsItsCounts_ItsRates_AndItsCosts()
    {
        var (cut, _) = Render(Run);

        var overall = cut.FindAll(".summary-mode")[0].QuerySelectorAll(".summary-row")[0];
        Assert.That(overall.QuerySelector(".s-n")!.TextContent, Is.EqualTo("8"));
        Assert.That(overall.QuerySelector(".s-passed")!.TextContent, Is.EqualTo("6 of 8 passed"));
        Assert.That(overall.QuerySelector(".s-rate")!.TextContent, Is.EqualTo("75%"));
        Assert.That(overall.QuerySelector(".s-at-k")!.TextContent, Is.EqualTo("100%"));
        Assert.That(overall.QuerySelector(".s-hat-k")!.TextContent, Is.EqualTo("50%"));
        Assert.That((overall.QuerySelector(".s-unknown")!.TextContent, overall.QuerySelector(".s-infra")!.TextContent), Is.EqualTo(("1", "0")));
        Assert.That(overall.QuerySelector(".s-cost-trial")!.TextContent, Does.StartWith("$0.01"));
        Assert.That(overall.QuerySelector(".s-cost-pass")!.TextContent, Does.StartWith("$0.01"));
    }

    [Test]
    public void ATooFewLine_ReadsTooFewForARate_NeverAPercentage_AndAnUnknownCostIsUnknown()
    {
        var (cut, _) = Render(Run);

        var few = cut.FindAll(".summary-mode")[0].QuerySelectorAll(".summary-row")[1];
        Assert.That(few.QuerySelector(".s-passed")!.TextContent, Is.EqualTo("2 of 3 passed · too few for a rate"));
        Assert.That(few.TextContent, Does.Not.Contain("%"), "no percentage of three trials");
        Assert.That(few.QuerySelector(".s-cost-trial")!.TextContent, Is.EqualTo("unknown"));
    }

    [Test]
    public void AZeroCostReadsDollarZero_AndNothingPassedHasNoCostPerPass()
    {
        var (cut, _) = Render(Run);

        var budget = cut.FindAll(".summary-mode")[0].QuerySelectorAll(".summary-row")[2];
        Assert.That(budget.QuerySelector(".s-cost-trial")!.TextContent, Is.EqualTo("$0"));
        Assert.That(budget.QuerySelector(".s-cost-pass")!.TextContent, Is.EqualTo("–"));
    }

    [Test]
    public void AnInfrastructureTrial_IsCountedApart_AndLeftOutOfWhatPassedIsOutOf()
    {
        var (cut, _) = Render(Run);

        var tools = cut.FindAll(".summary-mode")[1].QuerySelector(".summary-row")!;
        Assert.That(tools.QuerySelector(".s-passed")!.TextContent, Is.EqualTo("2 of 7 passed"));
        Assert.That(tools.QuerySelector(".s-infra")!.TextContent, Is.EqualTo("1"));
    }

    [Test]
    public void ASlicesTasks_LinkToTheirConversationsInTheRunExplorer()
    {
        var (cut, _) = Render(Run);

        var links = cut.FindAll(".summary-mode")[0].QuerySelectorAll(".summary-row")[0].QuerySelectorAll("a.s-task");
        Assert.That(links.Select(a => (a.TextContent, a.GetAttribute("href"))), Is.EqualTo(new[]
        {
            ("cook-af", "admin/benchmarks/run123456789?task=cook-af"),
            ("cook-en", "admin/benchmarks/run123456789?task=cook-en"),
        }));
    }

    [Test]
    public void ARunFromBefore5c_SaysSo_AndOffersTheReScore()
    {
        var (cut, handler) = Render("""{"run":{"id":"old123456789","check":"puls-cook","status":"done"},"summaries":null}""");

        Assert.That(cut.Find(".summary-none").TextContent, Does.Contain("no categories: re-score this run to see them"));
        Assert.That(cut.FindAll(".summary-mode"), Is.Empty);

        cut.Find("button.rescore").Click();

        cut.WaitForAssertion(() => Assert.That(handler.Requests.Select(r => (r.Method, r.Url)),
            Is.EqualTo(new[] { (HttpMethod.Post, "http://localhost/v1/admin/benchmarks/runs/old123456789/rescore") })));
    }

    // 6a.4 (Louis: "too flat"): the three rate cells carry a bar as wide as the rate, green for the pass rate and pass@k, orange for pass^k; a slice too few for a rate has no bar.
    [Test]
    public void TheRateCells_CarryABarAsWideAsTheRate_AndATooFewSliceHasNone()
    {
        var (cut, _) = Render(Run);
        var rows = cut.FindAll(".summary-row");

        Assert.That(rows[0].QuerySelector(".s-rate .rate-fill")!.GetAttribute("style"), Does.Contain("75%"));
        Assert.That(rows[0].QuerySelector(".s-at-k .rate-fill")!.GetAttribute("style"), Does.Contain("100%"));
        Assert.That(rows[0].QuerySelector(".s-hat-k .rate-fill")!.ClassList, Does.Contain("hat"));
        Assert.That(rows[0].QuerySelector(".s-hat-k .rate-fill")!.GetAttribute("style"), Does.Contain("50%"));
        Assert.That(rows[1].QuerySelectorAll(".rate-fill"), Is.Empty, "too few for a rate: no bar");
    }
}
