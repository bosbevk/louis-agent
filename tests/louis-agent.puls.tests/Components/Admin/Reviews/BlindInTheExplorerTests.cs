using LouisAgent.Testing;
using System.Text.Json;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Puls.Admin.Notes;
using LouisAgent.Puls.Admin.Reviews;
using LouisAgent.Puls.Components.Admin;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Puls.Tests.Components.Admin.Reviews;

/// <summary>
/// In the run explorer, a conversation of a run with a human review shows the review panel, and the judge's verdict
/// is in no markup of the page until the reviewer has graded: not in the panel, and not in the conversation lists'
/// chips, neither colour nor hover.
/// </summary>
public class BlindInTheExplorerTests
{
    /// <summary>The judge's reason, which must stay out of the page until the reviewer has graded.</summary>
    private const string JudgeReason = "feeds four with portions that add up";

    /// <summary>A run with a human review and one conversation.</summary>
    private static readonly string Run = SampleRole.Fill("""
                                                         {"run":{"id":"run1","check":"puls-cook","repeat":1,
                                                                 "graders":[{"name":"no-error","version":1,"kind":"FixedRule","question":null},{"name":"meal-for-people","version":1,"kind":"Model","question":null},
                                                                            {"name":"meal-for-people.human","version":1,"kind":"Human","question":"Is this a meal for the people asked for?"}]},
                                                          "modes":[{"name":"puls-cook","repeat":1,"toolsets":["web"]}],
                                                          "conversations":[{"mode":"puls-cook","repeat":1,"task":"bobotie","role":null,"errors":0,"outcome":"Pass",
                                                             "grades":[{"grader":"no-error","version":1,"score":1,"outcome":"Pass","reason":"fine"},
                                                                       {"grader":"meal-for-people","version":1,"score":1,"outcome":"Pass","reason":"__REASON__"},
                                                                       {"grader":"meal-for-people.human","version":1,"score":0,"outcome":"Pending","reason":"waits for a person"}],
                                                             "human_grades":[]}],
                                                          "turns":[{"mode":"puls-cook","repeat":1,"task":"bobotie","turn":1,"session":"s","question":"Wat kan ek vanaand kook?","answer":"Bobotie vir vier.","role":"<RoleAlpha>","seconds":3.0,
                                                                    "hosts":[],"error":null,"tools":{"calls":0,"failed":0,"reasons":[]}}],
                                                          "requests":[]}
                                                         """).Replace("__REASON__", JudgeReason);

    /// <summary>Renders the explorer as this reviewer (or nobody), opened at a task if one is given.</summary>
    private static IRenderedComponent<RunExplorer> Render(string? reviewer, string? openTask)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        // The reviewer's name is kept in the browser's storage; pretend this reviewer already typed theirs.
        if (reviewer is not null)
            ctx.JSInterop.Setup<string?>("localStorage.getItem", ReviewerState.StorageKey).SetResult(reviewer);

        // What the explorer needs: an API that answers nothing, who the reviewer is, and the notes state.
        ctx.Services.AddSingleton(new AgentApi(
            new HttpClient(new StubHandler(_ => new HttpResponseMessage()))
            {
                BaseAddress = new Uri("http://localhost/")
            }, new ChatSettings()));
        var state = new ReviewerState(ctx.JSInterop.JSRuntime);
        ctx.Services.AddSingleton(state);
        ctx.Services.AddSingleton(new RunNotesState());

        // Render it on the run. With no task to open we are at the run's tiles, so click through to the list of
        // conversations.
        var cut = ctx.Render<RunExplorer>(p => p.Add(x => x.Run, Stored(Run)).Add(x => x.OpenTask, openTask));
        if (openTask is null) cut.Find(".tile").Click();

        // Wait until the reviewer's name has loaded, so the blind rules know who is looking.
        cut.WaitForState(() => state.Name == reviewer || reviewer is null);
        return cut;
    }

    /// <summary>The run as the page reads it from the API: the contract's stored run reply.</summary>
    private static StoredRunReply Stored(string json) =>
        JsonSerializer.Deserialize<StoredRunReply>(json, AdminJson.SnakeCase)!;

    /// <summary>
    /// A conversation of a run with a human review shows the review panel, with its question, what was asked and the
    /// answer.
    /// </summary>
    [Test]
    public void AConversationOfARunWithAHumanReview_ShowsTheReviewPanel_WithItsQuestionAskAndAnswer()
    {
        var cut = Render("Louis", "bobotie");

        Assert.That(cut.Find(".review-question").TextContent, Is.EqualTo("Is this a meal for the people asked for?"));
        Assert.That(cut.Find(".review-ask").TextContent, Does.Contain("Wat kan ek vanaand kook?"));
        Assert.That(cut.Find(".review-answer").TextContent, Does.Contain("Bobotie vir vier."));
    }

    /// <summary>
    /// Before the reviewer grades, the judge's reason is nowhere in the page: not in the conversation, not in the
    /// list, not with no name set.
    /// </summary>
    [Test]
    public void BeforeTheReviewerGrades_TheJudgesReasonIsInNoMarkupOfThePage_AtTheConversationListEither()
    {
        Assert.That(Render("Louis", "bobotie").Markup, Does.Not.Contain(JudgeReason), "in the conversation");
        Assert.That(Render("Louis", null).Markup, Does.Not.Contain(JudgeReason), "in the list of conversations");
        Assert.That(Render(null, null).Markup, Does.Not.Contain(JudgeReason), "with no name set");
    }

    /// <summary>In the list, a hidden judge's chip has no colour, while the fixed rule's chip keeps its own.</summary>
    [Test]
    public void TheListsChipOfAHiddenJudge_HasNoColour_AndTheFixedRulesChipKeepsIt()
    {
        var cut = Render("Louis", null);

        var chips = cut.FindAll(".grade-chips .chip");
        Assert.That(chips.Single(c => c.TextContent == "meal-for-people").ClassList,
            Does.Not.Contain("ok").And.Not.Contain("bad").And.Not.Contain("warn"));
        Assert.That(chips.Single(c => c.TextContent == "no-error").ClassList, Does.Contain("ok"));
    }

    /// <summary>
    /// A conversation the review had nothing to ask about shows no panel, even though the run has a review.
    /// </summary>
    [Test]
    public void AConversationWithNoPendingReviewGrade_ShowsNoPanel_ThoughTheRunHasAReview()
    {
        // A control the judge had nothing to judge: the review grader gave it no grade (step 6a.4), so there is
        // nothing for a person to answer.
        var run = Run.Replace(
            """{"grader":"meal-for-people.human","version":1,"score":0,"outcome":"Pending","reason":"waits for a person"}""",
            """{"grader":"no-error","version":1,"score":1,"outcome":"Pass","reason":"again"}""");
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(new AgentApi(
            new HttpClient(new StubHandler(_ => new HttpResponseMessage()))
            {
                BaseAddress = new Uri("http://localhost/")
            }, new ChatSettings()));
        ctx.Services.AddSingleton(new ReviewerState(ctx.JSInterop.JSRuntime));
        ctx.Services.AddSingleton(new RunNotesState());

        var cut = ctx.Render<RunExplorer>(p => p.Add(x => x.Run, Stored(run)).Add(x => x.OpenTask, "bobotie"));

        Assert.That(cut.FindAll(".review-panel"), Is.Empty);
    }

    /// <summary>A run with no human review shows no panel, and the judge's chip as before.</summary>
    [Test]
    public void ARunWithNoHumanReview_ShowsNoPanel_AndTheJudgesChipAsItWas()
    {
        var run = Run.Replace(
            """{"name":"meal-for-people.human","version":1,"kind":"Human","question":"Is this a meal for the people asked for?"}""",
            """{"name":"x","version":1,"kind":"FixedRule","question":null}""");
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(new AgentApi(
            new HttpClient(new StubHandler(_ => new HttpResponseMessage()))
            {
                BaseAddress = new Uri("http://localhost/")
            }, new ChatSettings()));
        ctx.Services.AddSingleton(new ReviewerState(ctx.JSInterop.JSRuntime));
        ctx.Services.AddSingleton(new RunNotesState());

        var cut = ctx.Render<RunExplorer>(p => p.Add(x => x.Run, Stored(run)).Add(x => x.OpenTask, null));
        cut.Find(".tile").Click();

        Assert.That(cut.FindAll(".review-panel"), Is.Empty);
        Assert.That(cut.FindAll(".grade-chips .chip").Single(c => c.TextContent == "meal-for-people").ClassList,
            Does.Contain("ok"));
    }
}
