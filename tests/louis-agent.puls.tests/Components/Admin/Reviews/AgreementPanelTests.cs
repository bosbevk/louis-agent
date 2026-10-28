using System.Net;
using System.Text;
using LouisAgent.Api.Contracts.Admin.Reviews;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Puls.Components.Admin.Reviews;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Puls.Tests.Components.Admin.Reviews;

/// <summary>
/// The run's page shows, per judge, how often it agreed with the people (F18 step 6a.3): a small card per judge, each
/// class of your grades one plain sentence with a bar under it and its share, and a link to download the people's
/// labels as the zip of label files the API exports.
/// </summary>
public class AgreementPanelTests
{
    /// <summary>A judge's row: what it said to the answers the person passed, and to those they failed.</summary>
    private static AgreementRow Judge(string grader, (int Pass, int Fail, int Unknown) passed,
        (int Pass, int Fail, int Unknown) failed) =>
        new(grader, new CalibrationClassRow(passed.Pass, passed.Fail, passed.Unknown),
            new CalibrationClassRow(failed.Pass, failed.Fail, failed.Unknown));

    /// <summary>The cook's meal judge: 13 of 14 passes agreed, all 4 fails.</summary>
    private static readonly AgreementRow Cook = Judge("meal-for-people", (13, 1, 0), (0, 4, 0));

    /// <summary>The panel for these judges, with an API that serves a labels zip and no judge checks.</summary>
    private static (IRenderedComponent<AgreementPanel> Cut, StubHandler Handler, BunitContext Ctx) Render(
        params AgreementRow[] agreement)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        // The labels download gets a zip; the judge check's read (6a.5c) gets an empty list.
        var handler = new StubHandler(request => request.RequestUri!.AbsolutePath.EndsWith("/calibrations")
            ? new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"calibrations\":[]}", Encoding.UTF8, "application/json")
            }
            : new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([80, 75, 3, 4]) });
        ctx.Services.AddSingleton(new AgentApi(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") },
            new ChatSettings()));

        return (ctx.Render<AgreementPanel>(p => p.Add(x => x.Agreement, agreement).Add(x => x.RunId, "run1")),
            handler, ctx);
    }

    /// <summary>
    /// Each judge is a card (Louis, 2026-10-26: "I do not understand what I'm reading"): each class one plain
    /// sentence with a full-width bar under it and a short share beside the bar.
    /// </summary>
    [Test]
    public void EachJudgeIsACard_WithASentenceAndABarPerClassOfYourGrades()
    {
        var (cut, _, _) = Render(Cook);

        Assert.That(cut.Find(".agreement-judge h4").TextContent, Is.EqualTo("meal-for-people"));
        Assert.That(cut.Find(".agreement-pass .agreement-sentence").TextContent,
            Is.EqualTo("You passed 14 answers. The judge passed 13 of them."));
        Assert.That(cut.Find(".agreement-pass .rate-fill").GetAttribute("style"), Does.Contain("93%"));
        Assert.That(cut.Find(".agreement-pass .agreement-share").TextContent, Is.EqualTo("93%"));
        Assert.That(cut.Find(".agreement-fail .agreement-sentence").TextContent,
            Is.EqualTo("You failed 4 answers. The judge failed all 4."));
        Assert.That(cut.Find(".agreement-fail .agreement-share").TextContent, Is.EqualTo("100% · few labels"));
        Assert.That(cut.Find(".agreement-lead").TextContent, Does.Contain("see what you see"));
        Assert.That(cut.Markup, Does.Not.Contain("<table"));
    }

    /// <summary>A class nobody has graded says so, with an empty bar.</summary>
    [Test]
    public void AClassNobodyHasGraded_SaysSo_WithAnEmptyBar()
    {
        var (cut, _, _) = Render(Judge("g", (3, 0, 0), (0, 0, 0)));

        Assert.That(cut.Find(".agreement-fail .agreement-sentence").TextContent,
            Is.EqualTo("You have failed no answers yet."));
        Assert.That(cut.Find(".agreement-fail .agreement-share").TextContent, Is.EqualTo("no labels"));
        Assert.That(cut.Find(".agreement-fail .rate-fill").GetAttribute("style"), Does.Contain("0%"));
    }

    /// <summary>A run with no judge reviewed shows nothing, not an empty table.</summary>
    [Test]
    public void ARunWithNoJudgeReviewed_ShowsNothing_NotAnEmptyTable()
    {
        var (cut, _, _) = Render();

        Assert.That(cut.FindAll(".agreement-judge"), Is.Empty);
        Assert.That(cut.FindAll(".agreement-lead"), Is.Empty);
    }

    /// <summary>The download asks the API for the labels zip and hands it to the browser.</summary>
    [Test]
    public void TheDownload_AsksTheAPIForTheLabelsZip_AndHandsItToTheBrowser()
    {
        var (cut, handler, ctx) = Render(Cook);

        cut.Find(".agreement-download").Click();

        cut.WaitForAssertion(() => Assert.That(handler.Requests.Count(r => r.Url.Contains("/labels")), Is.EqualTo(1)));
        Assert.That(handler.Requests.Single(r => r.Url.Contains("/labels")).Url,
            Does.EndWith("v1/admin/benchmarks/runs/run1/labels?grader=meal-for-people"));
        cut.WaitForAssertion(() =>
            Assert.That(
                ctx.JSInterop.Invocations.Any(i =>
                    i.Identifier == "louisDownload" && (string)i.Arguments[0]! == "meal-for-people-labels-run1.zip"),
                Is.True));
    }

    /// <summary>With nobody graded yet there is no download.</summary>
    [Test]
    public void WithNobodyGradedYet_ThereIsNoDownload()
    {
        var (cut, _, _) = Render(Judge("g", (0, 0, 0), (0, 0, 0)));

        Assert.That(cut.FindAll(".agreement-download"), Is.Empty);
        Assert.That(cut.Find(".agreement-none").TextContent, Does.Contain("Nobody has graded"));
    }
}
