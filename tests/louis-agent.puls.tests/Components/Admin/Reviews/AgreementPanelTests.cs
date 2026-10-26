using System.Net;
using System.Text.Json;
using LouisAgent.Puls.Components.Admin.Reviews;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Puls.Tests.Components.Admin.Reviews;

// F18 step 6a.3: the run's page shows, per judge, how often it agreed with the people (the API's counts as a small table: the person's pass or fail against the judge's answer, and each class's
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

    [Test]
    public void EachJudgeHasATable_OfThePersonsPassAndFailAgainstTheJudgesAnswers()
    {
        var (cut, _, _) = Render(Cook);

        Assert.That(cut.Find(".agreement-judge h3").TextContent, Is.EqualTo("meal-for-people"));
        var rows = cut.FindAll(".agreement-table tbody tr");
        Assert.That(rows[0].QuerySelectorAll("td").Select(c => c.TextContent), Is.EqualTo(new[] { "You passed", "13", "1", "0", "13 of 14 (93%)" }));
        Assert.That(rows[1].QuerySelectorAll("td").Select(c => c.TextContent), Is.EqualTo(new[] { "You failed", "0", "4", "0", "4 of 4 (100%), few labels" }));
        Assert.That(cut.FindAll(".agreement-table thead th").Select(h => h.TextContent), Is.EqualTo(new[] { "", "Judge: pass", "Judge: fail", "Judge: can't tell", "Agreed" }));
    }

    [Test]
    public void AClassNobodyHasGraded_SaysNoLabels()
    {
        var (cut, _, _) = Render("""[{"grader":"g","person_pass":{"judge_pass":3,"judge_fail":0,"judge_unknown":0},"person_fail":{"judge_pass":0,"judge_fail":0,"judge_unknown":0}}]""");

        Assert.That(cut.FindAll(".agreement-table tbody tr")[1].QuerySelectorAll("td")[4].TextContent, Is.EqualTo("no labels"));
    }

    [Test]
    public void ARunWithNoJudgeReviewed_ShowsNothing_NotAnEmptyTable()
    {
        var (cut, _, _) = Render("[]");

        Assert.That(cut.FindAll(".agreement-judge"), Is.Empty);
        Assert.That(cut.Markup, Does.Not.Contain("<table"));
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
