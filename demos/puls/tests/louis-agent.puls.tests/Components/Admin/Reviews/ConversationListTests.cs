using LouisAgent.Testing;
using System.Text.Json;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Puls.Admin.Reviews;
using LouisAgent.Puls.Components.Admin;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Puls.Tests.Components.Admin.Reviews;

/// <summary>
/// The explorer's list of conversations, so you can see which ones you still have to check: a card per conversation
/// per mode, with a review pill in front of its chips (to review, reviewed, no review) and a filter that keeps only
/// the ones still to review.
/// </summary>
public class ConversationListTests
{
    /// <summary>
    /// A run with a human review and three conversations: one still to review, one this reviewer has graded, and a
    /// control the review had nothing to ask about.
    /// </summary>
    private static readonly string Run = SampleEval.Fill(SampleMode.Fill(SampleRole.Fill("""
        {"run":{"id":"run1","check":"<EvalBeta>","repeat":1,
                "graders":[{"name":"no-error","version":1,"kind":"FixedRule"},{"name":"meal-for-people","version":1,"kind":"Model"},{"name":"meal-for-people.human","version":2,"kind":"Human","question":"Is this a meal?"}]},
         "modes":[{"name":"<ModeBeta>","repeat":1,"toolsets":["web"]}],
         "conversations":[
           {"mode":"<ModeBeta>","repeat":1,"task":"bobotie","role":null,"errors":0,"outcome":"Pass",
            "grades":[{"grader":"no-error","version":1,"score":1,"outcome":"Pass","reason":"fine"},{"grader":"meal-for-people","version":1,"score":1,"outcome":"Pass","reason":"feeds"},{"grader":"meal-for-people.human","version":2,"score":0,"outcome":"Pending","reason":"waits"}],
            "human_grades":[]},
           {"mode":"<ModeBeta>","repeat":1,"task":"curry","role":null,"errors":0,"outcome":"Pass",
            "grades":[{"grader":"no-error","version":1,"score":1,"outcome":"Pass","reason":"fine"},{"grader":"meal-for-people","version":1,"score":1,"outcome":"Pass","reason":"feeds"},{"grader":"meal-for-people.human","version":2,"score":0,"outcome":"Pending","reason":"waits"}],
            "human_grades":[{"grader":"meal-for-people.human","model_grader":"meal-for-people","verdict":"pass","note":"","reviewer":"louis","at":"2026-10-26T09:00:00+00:00","latest":true}]},
           {"mode":"<ModeBeta>","repeat":1,"task":"control","role":null,"errors":0,"outcome":"Pass",
            "grades":[{"grader":"no-error","version":1,"score":1,"outcome":"Pass","reason":"fine"},{"grader":"meal-for-people","version":1,"score":1,"outcome":"Pass","reason":"not asked"}],
            "human_grades":[]}],
         "turns":[{"mode":"<ModeBeta>","repeat":1,"task":"bobotie","turn":1,"session":"s","question":"Wat kook ek?","answer":"Bobotie.","role":"<RoleAlpha>","seconds":3.0,"hosts":[],"error":null,"tools":{"calls":0,"failed":0,"reasons":[]}},
                  {"mode":"<ModeBeta>","repeat":1,"task":"curry","turn":1,"session":"s","question":"Kerrie?","answer":"Kerrie.","role":"<RoleAlpha>","seconds":3.0,"hosts":[],"error":null,"tools":{"calls":0,"failed":0,"reasons":[]}},
                  {"mode":"<ModeBeta>","repeat":1,"task":"control","turn":1,"session":"s","question":"Prima?","answer":"10,75%.","role":null,"seconds":3.0,"hosts":[],"error":null,"tools":{"calls":0,"failed":0,"reasons":[]}}],
         "requests":[]}
        """)));

    /// <summary>
    /// Renders the explorer's list of conversations as this reviewer (or nobody), and hands back who it is.
    /// </summary>
    private static (IRenderedComponent<RunExplorer> Cut, ReviewerState State) Render(string? reviewer)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        // The reviewer's name is kept in the browser's storage; pretend this reviewer already typed theirs.
        if (reviewer is not null)
            ctx.JSInterop.Setup<string?>("localStorage.getItem", ReviewerState.StorageKey).SetResult(reviewer);

        // What the explorer needs: an API that answers nothing, and who the reviewer is.
        ctx.Services.AddSingleton(new AgentApi(
            new HttpClient(new StubHandler(_ => new HttpResponseMessage()))
            {
                BaseAddress = new Uri("http://localhost/")
            }, new ChatSettings()));
        var state = new ReviewerState(ctx.JSInterop.JSRuntime);
        ctx.Services.AddSingleton(state);

        // Render it on the run as the page reads it from the API, then click through from the run's tiles to the list.
        var run = JsonSerializer.Deserialize<StoredRunReply>(Run, AdminJson.SnakeCase)!;
        var cut = ctx.Render<RunExplorer>(p => p.Add(x => x.Run, run));
        cut.Find(".tile").Click();
        cut.WaitForState(() => state.Name == reviewer || reviewer is null);
        return (cut, state);
    }

    /// <summary>Each card's task with what its review pill says.</summary>
    private static Dictionary<string, string> Pills(IRenderedComponent<RunExplorer> cut) =>
        cut.FindAll(".conv-card").ToDictionary(card => card.QuerySelector("code")!.TextContent,
            card => card.QuerySelector(".review-pill")!.TextContent);

    /// <summary>Every conversation is a card whose pill says whether you still have to grade it.</summary>
    [Test]
    public void EachConversationIsACard_WithAReviewPill_ThatSaysWhetherThisReviewerStillHasToGradeIt()
    {
        var (cut, _) = Render("Louis");

        Assert.That(Pills(cut),
            Is.EqualTo(new Dictionary<string, string>
            {
                ["bobotie"] = "to review",
                ["curry"] = "reviewed ✓",
                ["control"] = "no review"
            }));
        Assert.That(cut.Find(".conv-card[data-review=to-review] code").TextContent, Is.EqualTo("bobotie"));
    }

    /// <summary>The filter keeps only what you still have to review, and "all" brings the rest back.</summary>
    [Test]
    public void TheFilter_KeepsOnlyTheOnesStillToReview_AndAllBringsThemBack()
    {
        var (cut, _) = Render("Louis");

        cut.Find(".conv-filter [data-filter=to-review]").Click();
        Assert.That(cut.FindAll(".conv-card").Select(c => c.QuerySelector("code")!.TextContent),
            Is.EqualTo(new[] { "bobotie" }));

        cut.Find(".conv-filter [data-filter=all]").Click();
        Assert.That(cut.FindAll(".conv-card"), Has.Count.EqualTo(3));
    }

    /// <summary>A grade you just gave turns its pill to reviewed straight away, with no reload.</summary>
    [Test]
    public void AGradeGivenThisSession_TurnsThePillToReviewed_AtOnce()
    {
        var (cut, state) = Render("Louis");

        state.Record(new ConversationRef("run1", SampleMode.Beta, 1, "bobotie"), "meal-for-people");

        cut.WaitForAssertion(() => Assert.That(Pills(cut)["bobotie"], Is.EqualTo("reviewed ✓")));
    }

    /// <summary>A run that cost nothing (a re-score) shows no cost bars.</summary>
    [Test]
    public void TheCostStrip_ShowsOnlyWhenTheRunCostSomething() =>
        Assert.That(Render("Louis").Cut.FindAll(".conv-strip"), Is.Empty,
            "a re-score: R0 everywhere, so no empty bars");
}
