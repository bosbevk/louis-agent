using System.Text.Json;
using LouisAgent.Puls.Admin.Reviews;
using LouisAgent.Puls.Components.Admin;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Puls.Tests.Components.Admin.Reviews;

// F18 step 6a.4d (Louis: "make this screen better, I need to know which ones to check"): the explorer's conversation list is a card per conversation per mode, the chips in one line
// across its width, a review pill in front of them (to review / reviewed / no review), and a filter that keeps only the ones still to review. Written first: the list had a narrow chip
// column beside an empty cost bar and no pill.
public class ConversationListTests
{
    private const string Run = """
        {"run":{"id":"run1","check":"puls-cook","repeat":1,
                "graders":[{"name":"no-error","version":1,"kind":"FixedRule"},{"name":"meal-for-people","version":1,"kind":"Model"},{"name":"meal-for-people.human","version":2,"kind":"Human","question":"Is this a meal?"}]},
         "modes":[{"name":"puls-cook","repeat":1,"toolsets":["web"]}],
         "conversations":[
           {"mode":"puls-cook","repeat":1,"task":"bobotie","role":null,"scores":{},"errors":0,"outcome":"Pass",
            "grades":[{"grader":"no-error","version":1,"score":1,"outcome":"Pass","reason":"fine"},{"grader":"meal-for-people","version":1,"score":1,"outcome":"Pass","reason":"feeds"},{"grader":"meal-for-people.human","version":2,"score":0,"outcome":"Pending","reason":"waits"}],
            "human_grades":[]},
           {"mode":"puls-cook","repeat":1,"task":"curry","role":null,"scores":{},"errors":0,"outcome":"Pass",
            "grades":[{"grader":"no-error","version":1,"score":1,"outcome":"Pass","reason":"fine"},{"grader":"meal-for-people","version":1,"score":1,"outcome":"Pass","reason":"feeds"},{"grader":"meal-for-people.human","version":2,"score":0,"outcome":"Pending","reason":"waits"}],
            "human_grades":[{"grader":"meal-for-people.human","model_grader":"meal-for-people","verdict":"pass","note":"","reviewer":"louis","at":"2026-10-26T09:00:00+00:00","latest":true}]},
           {"mode":"puls-cook","repeat":1,"task":"control","role":null,"scores":{},"errors":0,"outcome":"Pass",
            "grades":[{"grader":"no-error","version":1,"score":1,"outcome":"Pass","reason":"fine"},{"grader":"meal-for-people","version":1,"score":1,"outcome":"Pass","reason":"not asked"}],
            "human_grades":[]}],
         "turns":[{"mode":"puls-cook","repeat":1,"task":"bobotie","turn":1,"session":"s","question":"Wat kook ek?","answer":"Bobotie.","role":"puls-cook","seconds":3.0,"hosts":[],"error":null,"tools":{"calls":0,"failed":0,"reasons":[]}},
                  {"mode":"puls-cook","repeat":1,"task":"curry","turn":1,"session":"s","question":"Kerrie?","answer":"Kerrie.","role":"puls-cook","seconds":3.0,"hosts":[],"error":null,"tools":{"calls":0,"failed":0,"reasons":[]}},
                  {"mode":"puls-cook","repeat":1,"task":"control","turn":1,"session":"s","question":"Prima?","answer":"10,75%.","role":null,"seconds":3.0,"hosts":[],"error":null,"tools":{"calls":0,"failed":0,"reasons":[]}}],
         "requests":[]}
        """;

    private static (IRenderedComponent<RunExplorer> Cut, ReviewerState State) Render(string? reviewer)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        if (reviewer is not null) ctx.JSInterop.Setup<string?>("localStorage.getItem", ReviewerState.StorageKey).SetResult(reviewer);
        ctx.Services.AddSingleton(new AgentApi(new HttpClient(new StubHandler(_ => new HttpResponseMessage())) { BaseAddress = new Uri("http://localhost/") }, new ChatSettings()));
        var state = new ReviewerState(ctx.JSInterop.JSRuntime);
        ctx.Services.AddSingleton(state);
        var cut = ctx.Render<RunExplorer>(p => p.Add(x => x.Run, JsonDocument.Parse(Run).RootElement.Clone()));
        cut.Find(".tile").Click();
        cut.WaitForState(() => state.Name == reviewer || reviewer is null);
        return (cut, state);
    }

    private static Dictionary<string, string> Pills(IRenderedComponent<RunExplorer> cut) =>
        cut.FindAll(".conv-card").ToDictionary(card => card.QuerySelector("code")!.TextContent, card => card.QuerySelector(".review-pill")!.TextContent);

    [Test]
    public void EachConversationIsACard_WithAReviewPill_ThatSaysWhetherThisReviewerStillHasToGradeIt()
    {
        var (cut, _) = Render("Louis");

        Assert.That(Pills(cut), Is.EqualTo(new Dictionary<string, string> { ["bobotie"] = "to review", ["curry"] = "reviewed ✓", ["control"] = "no review" }));
        Assert.That(cut.Find(".conv-card[data-review=to-review] code").TextContent, Is.EqualTo("bobotie"));
    }

    [Test]
    public void TheFilter_KeepsOnlyTheOnesStillToReview_AndAllBringsThemBack()
    {
        var (cut, _) = Render("Louis");

        cut.Find(".conv-filter [data-filter=to-review]").Click();
        Assert.That(cut.FindAll(".conv-card").Select(c => c.QuerySelector("code")!.TextContent), Is.EqualTo(new[] { "bobotie" }));

        cut.Find(".conv-filter [data-filter=all]").Click();
        Assert.That(cut.FindAll(".conv-card"), Has.Count.EqualTo(3));
    }

    [Test]
    public void AGradeGivenThisSession_TurnsThePillToReviewed_AtOnce()
    {
        var (cut, state) = Render("Louis");

        state.Record(new ConversationRef("run1", "puls-cook", 1, "bobotie"), "meal-for-people");

        cut.WaitForAssertion(() => Assert.That(Pills(cut)["bobotie"], Is.EqualTo("reviewed ✓")));
    }

    [Test]
    public void TheCostStrip_ShowsOnlyWhenTheRunCostSomething() =>
        Assert.That(Render("Louis").Cut.FindAll(".conv-strip"), Is.Empty, "a re-score: R0 everywhere, so no empty bars");
}
