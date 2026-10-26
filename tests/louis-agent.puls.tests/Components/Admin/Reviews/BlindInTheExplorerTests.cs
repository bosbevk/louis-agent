using System.Text.Json;
using LouisAgent.Puls.Admin.Reviews;
using LouisAgent.Puls.Components.Admin;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Puls.Tests.Components.Admin.Reviews;

// F18 step 6a.3 (Louis's pick: "hide the judge's chip too"): in the run explorer, a conversation of a run with a human review shows the review panel, and the judge's verdict is in NO
// markup of the page until the reviewer has graded: not in the panel, not in the conversation lists' chips (colour or hover). Written first: the explorer knew nothing of reviews.
public class BlindInTheExplorerTests
{
    private const string JudgeReason = "feeds four with portions that add up";

    private static readonly string Run = """
        {"run":{"id":"run1","check":"puls-cook","repeat":1,
                "graders":[{"name":"no-error","version":1,"kind":"FixedRule","question":null},{"name":"meal-for-people","version":1,"kind":"Model","question":null},
                           {"name":"meal-for-people.human","version":1,"kind":"Human","question":"Is this a meal for the people asked for?"}]},
         "modes":[{"name":"puls-cook","repeat":1,"toolsets":["web"]}],
         "conversations":[{"mode":"puls-cook","repeat":1,"task":"bobotie","role":null,"scores":{},"errors":0,"outcome":"Pass",
            "grades":[{"grader":"no-error","version":1,"score":1,"outcome":"Pass","reason":"fine"},
                      {"grader":"meal-for-people","version":1,"score":1,"outcome":"Pass","reason":"__REASON__"},
                      {"grader":"meal-for-people.human","version":1,"score":0,"outcome":"Pending","reason":"waits for a person"}],
            "human_grades":[]}],
         "turns":[{"mode":"puls-cook","repeat":1,"task":"bobotie","turn":1,"session":"s","question":"Wat kan ek vanaand kook?","answer":"Bobotie vir vier.","role":"puls-cook","seconds":3.0,
                   "hosts":[],"error":null,"tools":{"calls":0,"failed":0,"reasons":[]}}],
         "requests":[]}
        """.Replace("__REASON__", JudgeReason);

    private static IRenderedComponent<RunExplorer> Render(string? reviewer, string? openTask)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        if (reviewer is not null) ctx.JSInterop.Setup<string?>("localStorage.getItem", ReviewerState.StorageKey).SetResult(reviewer);
        ctx.Services.AddSingleton(new AgentApi(new HttpClient(new StubHandler(_ => new HttpResponseMessage())) { BaseAddress = new Uri("http://localhost/") }, new ChatSettings()));
        var state = new ReviewerState(ctx.JSInterop.JSRuntime);
        ctx.Services.AddSingleton(state);
        var cut = ctx.Render<RunExplorer>(p => p.Add(x => x.Run, JsonDocument.Parse(Run).RootElement.Clone()).Add(x => x.OpenTask, openTask));
        if (openTask is null) cut.Find(".tile").Click(); // the list of conversations is the level below the run's tiles
        cut.WaitForState(() => state.Name == reviewer || reviewer is null);
        return cut;
    }

    [Test]
    public void AConversationOfARunWithAHumanReview_ShowsTheReviewPanel_WithItsQuestionAskAndAnswer()
    {
        var cut = Render("Louis", "bobotie");

        Assert.That(cut.Find(".review-question").TextContent, Is.EqualTo("Is this a meal for the people asked for?"));
        Assert.That(cut.Find(".review-ask").TextContent, Does.Contain("Wat kan ek vanaand kook?"));
        Assert.That(cut.Find(".review-answer").TextContent, Does.Contain("Bobotie vir vier."));
    }

    [Test]
    public void BeforeTheReviewerGrades_TheJudgesReasonIsInNoMarkupOfThePage_AtTheConversationListEither()
    {
        Assert.That(Render("Louis", "bobotie").Markup, Does.Not.Contain(JudgeReason), "in the conversation");
        Assert.That(Render("Louis", null).Markup, Does.Not.Contain(JudgeReason), "in the list of conversations");
        Assert.That(Render(null, null).Markup, Does.Not.Contain(JudgeReason), "with no name set");
    }

    [Test]
    public void TheListsChipOfAHiddenJudge_HasNoColour_AndTheFixedRulesChipKeepsIt()
    {
        var cut = Render("Louis", null);

        var chips = cut.FindAll(".grade-chips .chip");
        Assert.That(chips.Single(c => c.TextContent == "meal-for-people").ClassList, Does.Not.Contain("ok").And.Not.Contain("bad").And.Not.Contain("warn"));
        Assert.That(chips.Single(c => c.TextContent == "no-error").ClassList, Does.Contain("ok"));
    }

    [Test]
    public void AConversationWithNoPendingReviewGrade_ShowsNoPanel_ThoughTheRunHasAReview()
    {
        // A control the judge had nothing to judge: the review grader gave it no grade (step 6a.4), so there is nothing for a person to answer.
        var run = Run.Replace("""{"grader":"meal-for-people.human","version":1,"score":0,"outcome":"Pending","reason":"waits for a person"}""", """{"grader":"no-error","version":1,"score":1,"outcome":"Pass","reason":"again"}""");
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(new AgentApi(new HttpClient(new StubHandler(_ => new HttpResponseMessage())) { BaseAddress = new Uri("http://localhost/") }, new ChatSettings()));
        ctx.Services.AddSingleton(new ReviewerState(ctx.JSInterop.JSRuntime));

        var cut = ctx.Render<RunExplorer>(p => p.Add(x => x.Run, JsonDocument.Parse(run).RootElement.Clone()).Add(x => x.OpenTask, "bobotie"));

        Assert.That(cut.FindAll(".review-panel"), Is.Empty);
    }

    [Test]
    public void ARunWithNoHumanReview_ShowsNoPanel_AndTheJudgesChipAsItWas()
    {
        var run = Run.Replace("""{"name":"meal-for-people.human","version":1,"kind":"Human","question":"Is this a meal for the people asked for?"}""", """{"name":"x","version":1,"kind":"FixedRule","question":null}""");
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(new AgentApi(new HttpClient(new StubHandler(_ => new HttpResponseMessage())) { BaseAddress = new Uri("http://localhost/") }, new ChatSettings()));
        ctx.Services.AddSingleton(new ReviewerState(ctx.JSInterop.JSRuntime));

        var cut = ctx.Render<RunExplorer>(p => p.Add(x => x.Run, JsonDocument.Parse(run).RootElement.Clone()).Add(x => x.OpenTask, null));
        cut.Find(".tile").Click();

        Assert.That(cut.FindAll(".review-panel"), Is.Empty);
        Assert.That(cut.FindAll(".grade-chips .chip").Single(c => c.TextContent == "meal-for-people").ClassList, Does.Contain("ok"));
    }
}
