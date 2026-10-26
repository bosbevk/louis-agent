using System.Net;
using System.Text.Json;
using LouisAgent.Puls.Components.Admin.Reviews;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Puls.Tests.Components.Admin.Reviews;

// F18 step 6a.3: the run's page shows, per judge, how often it agreed with the people (the API's counts as a row per judge: the person's pass or fail against the judge's answer, and each class's
// share, "few labels" under ten), with a link to download the people's labels as the zip of label files the API exports. Written first: the panel did not exist.
public class AgreementPanelTests
{
    private static JsonElement Run(string agreement) =>
        JsonDocument.Parse("{\"run\":{\"id\":\"run1\"},\"agreement\":" + agreement + "}").RootElement.Clone();

    private const string Cook = """
        [{"grader":"meal-for-people","person_pass":{"judge_pass":13,"judge_fail":1,"judge_unknown":0},"person_fail":{"judge_pass":0,"judge_fail":4,"judge_unknown":0}}]
        """;

    private static (IRenderedComponent<AgreementPanel> Cut, StubHandler Handler, BunitContext Ctx) Render(string agreement)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([80, 75, 3, 4]) });
        ctx.Services.AddSingleton(new AgentApi(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }, new ChatSettings()));
        return (ctx.Render<AgreementPanel>(p => p.Add(x => x.Run, Run(agreement)).Add(x => x.RunId, "run1")), handler, ctx);
    }

    // Louis, 2026-10-26: "like the pass rates": a row per judge, with a bar per class of the people's grades (what you passed, what you failed) that shows the share the judge agreed,
    // the 2x2 counts on the bar's hover, and the download as a chip in the row.
    // Louis, 2026-10-26 ("I do not understand what I'm reading"): a small card per judge; each class is one plain sentence with a full-width bar under it and a short share beside the bar.
    [Test]
    public void EachJudgeIsACard_WithASentenceAndABarPerClassOfYourGrades()
    {
        var (cut, _, _) = Render(Cook);

        Assert.That(cut.Find(".agreement-judge h4").TextContent, Is.EqualTo("meal-for-people"));
        Assert.That(cut.Find(".agreement-pass .agreement-sentence").TextContent, Is.EqualTo("You passed 14 answers. The judge passed 13 of them."));
        Assert.That(cut.Find(".agreement-pass .rate-fill").GetAttribute("style"), Does.Contain("93%"));
        Assert.That(cut.Find(".agreement-pass .agreement-share").TextContent, Is.EqualTo("93%"));
        Assert.That(cut.Find(".agreement-fail .agreement-sentence").TextContent, Is.EqualTo("You failed 4 answers. The judge failed all 4."));
        Assert.That(cut.Find(".agreement-fail .agreement-share").TextContent, Is.EqualTo("100% · few labels"));
        Assert.That(cut.Find(".agreement-lead").TextContent, Does.Contain("see what you see"));
        Assert.That(cut.Markup, Does.Not.Contain("<table"));
    }

    [Test]
    public void AClassNobodyHasGraded_SaysSo_WithAnEmptyBar()
    {
        var (cut, _, _) = Render("""[{"grader":"g","person_pass":{"judge_pass":3,"judge_fail":0,"judge_unknown":0},"person_fail":{"judge_pass":0,"judge_fail":0,"judge_unknown":0}}]""");

        Assert.That(cut.Find(".agreement-fail .agreement-sentence").TextContent, Is.EqualTo("You have failed no answers yet."));
        Assert.That(cut.Find(".agreement-fail .agreement-share").TextContent, Is.EqualTo("no labels"));
        Assert.That(cut.Find(".agreement-fail .rate-fill").GetAttribute("style"), Does.Contain("0%"));
    }

    [Test]
    public void ARunWithNoJudgeReviewed_ShowsNothing_NotAnEmptyTable()
    {
        var (cut, _, _) = Render("[]");

        Assert.That(cut.FindAll(".agreement-judge"), Is.Empty);
        Assert.That(cut.FindAll(".agreement-lead"), Is.Empty);
    }

    [Test]
    public void TheDownload_AsksTheAPIForTheLabelsZip_AndHandsItToTheBrowser()
    {
        var (cut, handler, ctx) = Render(Cook);

        cut.Find(".agreement-download").Click();

        cut.WaitForAssertion(() => Assert.That(handler.Requests, Has.Count.EqualTo(1)));
        Assert.That(handler.Requests.Single().Url, Does.EndWith("v1/admin/benchmarks/runs/run1/labels?grader=meal-for-people"));
        cut.WaitForAssertion(() => Assert.That(ctx.JSInterop.Invocations.Any(i => i.Identifier == "louisDownload" && (string)i.Arguments[0]! == "meal-for-people-labels-run1.zip"), Is.True));
    }

    [Test]
    public void WithNobodyGradedYet_ThereIsNoDownload()
    {
        var (cut, _, _) = Render("""[{"grader":"g","person_pass":{"judge_pass":0,"judge_fail":0,"judge_unknown":0},"person_fail":{"judge_pass":0,"judge_fail":0,"judge_unknown":0}}]""");

        Assert.That(cut.FindAll(".agreement-download"), Is.Empty);
        Assert.That(cut.Find(".agreement-none").TextContent, Does.Contain("Nobody has graded"));
    }
}
