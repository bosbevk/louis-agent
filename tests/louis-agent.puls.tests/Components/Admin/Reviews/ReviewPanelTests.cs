using System.Net;
using System.Text;
using System.Text.Json;
using LouisAgent.Puls.Admin.Grades;
using LouisAgent.Puls.Admin.Reviews;
using LouisAgent.Puls.Components.Admin.Reviews;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Puls.Tests.Components.Admin.Reviews;

// F18 step 6a.3: the review panel of a conversation. It shows the question the person answers (read from the run's first line), the ask, the answer, three buttons (pass, fail, unsure)
// and a note, and is BLIND: before this reviewer has graded the conversation the judge's verdict and reason are not in the markup at all (not hidden by style: absent); after, they are,
// with whether the person agreed. The reviewer's name is asked once and kept in the browser; without it the buttons are disabled and a line says why. Written first: none of it existed.
public class ReviewPanelTests
{
    private const string Question = "Is this a meal for the people asked for?";
    private const string JudgeReason = "feeds four with portions that add up";

    private static readonly GraderRow Review = new("meal-for-people.human", 1, "Human", Question);
    private static readonly ConversationRef Bobotie = new("run1", "puls-cook", 1, "bobotie");

    private static ConversationGrades Grades(GradeOutcome judge = GradeOutcome.Pass, params HumanGradeRow[] humans) =>
        new([new GradeRow("no-error", 1, 1, GradeOutcome.Pass, "fine"), new GradeRow("meal-for-people", 1, judge == GradeOutcome.Pass ? 1 : 0, judge, JudgeReason),
             new GradeRow("meal-for-people.human", 1, 0, GradeOutcome.Pending, "waits")], GradeOutcome.Pass, humans);

    private sealed record Rendered(IRenderedComponent<ReviewPanel> Cut, StubHandler Handler, ReviewerState State);

    private static Rendered Render(string? reviewer, ConversationGrades grades, HttpStatusCode put = HttpStatusCode.OK)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        if (reviewer is not null) ctx.JSInterop.Setup<string?>("localStorage.getItem", ReviewerState.StorageKey).SetResult(reviewer);
        var handler = new StubHandler(_ => new HttpResponseMessage(put)
        {
            Content = new StringContent(put == HttpStatusCode.OK ? """{"verdict":"pass"}""" : """{"type":"error","error":{"type":"invalid_request_error","message":"A review needs the name of who gave it."}}""",
                Encoding.UTF8, "application/json"),
        });
        ctx.Services.AddSingleton(new AgentApi(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }, new ChatSettings()));
        var state = new ReviewerState(ctx.JSInterop.JSRuntime);
        ctx.Services.AddSingleton(state);
        var cut = ctx.Render<ReviewPanel>(p => p.Add(x => x.Conversation, Bobotie).Add(x => x.Review, Review)
            .Add(x => x.Asked, ["Wat kan ek vanaand vir vier mense kook?", "Maak dit eerder vir ses."]).Add(x => x.Answer, "Bobotie vir ses: 'n hoofgereg.").Add(x => x.Grades, grades));
        cut.WaitForState(() => state.Name == reviewer || reviewer is null);
        return new Rendered(cut, handler, state);
    }

    [Test]
    public void ItShowsTheQuestion_TheAsk_AndTheAnswer_WithThreeButtonsAndANote()
    {
        var (cut, _, _) = Render("Louis", Grades());

        Assert.That(cut.Find(".review-question").TextContent, Is.EqualTo(Question));
        Assert.That(cut.Find(".review-ask").TextContent, Does.Contain("Wat kan ek vanaand vir vier mense kook?").And.Contain("Maak dit eerder vir ses."));
        Assert.That(cut.Find(".review-answer").TextContent, Does.Contain("Bobotie vir ses"));
        Assert.That(new[] { ".review-pass", ".review-fail", ".review-unsure" }.Select(s => cut.Find(s).HasAttribute("disabled")), Is.EqualTo(new[] { false, false, false }));
        Assert.That(cut.Find(".review-note").TagName, Is.EqualTo("TEXTAREA"));
        Assert.That(cut.Find(".review-note").GetAttribute("maxlength"), Is.EqualTo("2000"), "the limit the API holds a note to (6a.3c: ReviewNote.MaxLength)");
    }

    [Test]
    public void BeforeThePersonGrades_TheJudgesVerdictAndReasonAreNotInTheMarkup_AtAll()
    {
        var (cut, _, _) = Render("Louis", Grades());

        Assert.That(cut.Markup, Does.Not.Contain(JudgeReason));
        Assert.That(cut.FindAll(".review-judge"), Is.Empty);
        Assert.That(cut.FindAll(".review-result"), Is.Empty);
    }

    [Test]
    public void WithNoName_TheButtonsAreDisabled_AndALineSaysWhy_AndTheJudgeStaysHidden()
    {
        var (cut, _, _) = Render(null, Grades());

        Assert.That(new[] { ".review-pass", ".review-fail", ".review-unsure" }.All(s => cut.Find(s).HasAttribute("disabled")), Is.True);
        Assert.That(cut.Find(".review-need-name").TextContent, Does.Contain("name"));
        Assert.That(cut.Markup, Does.Not.Contain(JudgeReason));
    }

    [Test]
    public void TypingANameOnce_EnablesTheButtons_AndKeepsItInTheBrowser()
    {
        var (cut, _, state) = Render(null, Grades());

        cut.Find(".review-name").Change("  Louis ");
        cut.Find(".review-name-keep").Click();

        cut.WaitForAssertion(() => Assert.That(cut.Find(".review-pass").HasAttribute("disabled"), Is.False));
        Assert.That(state.Name, Is.EqualTo("Louis"));
        Assert.That(cut.FindAll(".review-need-name"), Is.Empty);
    }

    [Test]
    public void APass_IsSentAsAPut_ToTheReviewsRoute_WithTheVerdictTheNoteAndTheName()
    {
        var (cut, handler, _) = Render("Louis", Grades());
        cut.Find(".review-note").Change("a main dish, feeds six");

        cut.Find(".review-pass").Click();

        cut.WaitForAssertion(() => Assert.That(handler.Requests, Has.Count.EqualTo(1)));
        var (method, url, body) = handler.Requests.Single();
        Assert.That(method, Is.EqualTo(HttpMethod.Put));
        Assert.That(url, Does.EndWith("v1/admin/benchmarks/runs/run1/reviews/puls-cook/1/bobotie/meal-for-people.human"));
        var sent = JsonDocument.Parse(body!).RootElement;
        Assert.That((sent.GetProperty("verdict").GetString(), sent.GetProperty("note").GetString(), sent.GetProperty("reviewer").GetString()),
            Is.EqualTo(("pass", "a main dish, feeds six", "Louis")));
    }

    [Test]
    public void AfterGrading_TheJudgesVerdictAndReasonAppear_WithYouAgreed()
    {
        var (cut, _, state) = Render("Louis", Grades(GradeOutcome.Pass));

        cut.Find(".review-pass").Click();

        cut.WaitForAssertion(() => Assert.That(cut.Find(".review-result").TextContent, Does.Contain("You agreed")));
        Assert.That(cut.Find(".review-judge").TextContent, Does.Contain("pass").And.Contain(JudgeReason));
        Assert.That(state.Graded(Bobotie, "meal-for-people"), Is.True, "so the judge's chip shows again everywhere");
    }

    [Test]
    public void AfterGrading_ADifferentVerdictFromTheJudge_ReadsYouDisagreed()
    {
        var (cut, _, _) = Render("Louis", Grades(GradeOutcome.Fail));

        cut.Find(".review-pass").Click();

        cut.WaitForAssertion(() => Assert.That(cut.Find(".review-result").TextContent, Does.Contain("You disagreed")));
    }

    [Test]
    public void AnUnsure_ShowsTheJudgeToo_AndSaysYouWereUnsure()
    {
        var (cut, _, _) = Render("Louis", Grades());

        cut.Find(".review-unsure").Click();

        cut.WaitForAssertion(() => Assert.That(cut.Find(".review-result").TextContent, Does.Contain("unsure")));
        Assert.That(cut.Find(".review-judge").TextContent, Does.Contain(JudgeReason));
    }

    [Test]
    public void AConversationThisReviewerAlreadyGraded_ShowsTheJudgeAtOnce_ThoughNotAnotherReviewers()
    {
        var mine = new HumanGradeRow("meal-for-people.human", "meal-for-people", "pass", "", "Louis", DateTimeOffset.UnixEpoch);
        var anna = mine with { Reviewer = "Anna" };

        Assert.That(Render("Louis", Grades(GradeOutcome.Pass, mine)).Cut.Find(".review-judge").TextContent, Does.Contain(JudgeReason));
        Assert.That(Render("Louis", Grades(GradeOutcome.Pass, anna)).Cut.Markup, Does.Not.Contain(JudgeReason));
    }

    [Test]
    public void AGradeTheAPIRefuses_ShowsItsMessage_AndTheJudgeStaysHidden()
    {
        var (cut, _, state) = Render("Louis", Grades(), HttpStatusCode.BadRequest);

        cut.Find(".review-pass").Click();

        cut.WaitForAssertion(() => Assert.That(cut.Find(".review-error").TextContent, Does.Contain("A review needs the name")));
        Assert.That(cut.Markup, Does.Not.Contain(JudgeReason));
        Assert.That(state.Graded(Bobotie, "meal-for-people"), Is.False);
    }

    // Louis, 2026-10-26: the pick must stay pressed. The three buttons are one segmented radio group, as the explorer's repeat picker is, and the chosen one is aria-checked:
    // right after the click, and again on a reload from the stored grade.
    [Test]
    public void ThePickStaysPressed_AsTheCheckedOneOfASegmentedGroup_AfterTheClickAndFromAStoredGrade()
    {
        var (cut, _, _) = Render("Louis", Grades());
        Assert.That(cut.FindAll(".review-buttons [role=radio]"), Has.Count.EqualTo(3));
        Assert.That(cut.FindAll(".review-buttons [aria-checked=true]"), Is.Empty, "nothing chosen yet");

        cut.Find(".review-fail").Click();

        cut.WaitForAssertion(() => Assert.That(cut.Find(".review-fail").GetAttribute("aria-checked"), Is.EqualTo("true")));
        Assert.That(cut.Find(".review-fail").ClassList, Does.Contain("active"));
        Assert.That(cut.Find(".review-pass").GetAttribute("aria-checked"), Is.EqualTo("false"));

        var stored = new HumanGradeRow("meal-for-people.human", "meal-for-people", "unsure", "", "Louis", DateTimeOffset.UnixEpoch);
        Assert.That(Render("Louis", Grades(GradeOutcome.Pass, stored)).Cut.Find(".review-unsure").GetAttribute("aria-checked"), Is.EqualTo("true"));
    }

    // 6a.4 (Louis: the grades went out under a name typed in another tab): with a name kept, the panel says whose grades these are and offers "not you?", which lets the name go.
    [Test]
    public void WithANameKept_ThePanelSaysWhoIsGrading_AndNotYouLetsTheNameGo()
    {
        var (cut, _, state) = Render("Test", Grades());
        Assert.That(cut.Find(".review-who").TextContent, Does.Contain("Test"));

        cut.Find(".review-change-name").Click();

        cut.WaitForAssertion(() => Assert.That(cut.FindAll(".review-need-name"), Has.Count.EqualTo(1)));
        Assert.That(state.Name, Is.Null);
        Assert.That(cut.Find(".review-pass").HasAttribute("disabled"), Is.True);
    }

    [Test]
    public void ARunWithNoQuestionOnItsFirstLine_StillAsksSomething_TheGradersName()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(new AgentApi(new HttpClient(new StubHandler(_ => new HttpResponseMessage())) { BaseAddress = new Uri("http://localhost/") }, new ChatSettings()));
        ctx.Services.AddSingleton(new ReviewerState(ctx.JSInterop.JSRuntime));

        var cut = ctx.Render<ReviewPanel>(p => p.Add(x => x.Conversation, Bobotie).Add(x => x.Review, new GraderRow("meal-for-people.human", 1, "Human"))
            .Add(x => x.Asked, ["q"]).Add(x => x.Answer, "a").Add(x => x.Grades, Grades()));

        Assert.That(cut.Find(".review-question").TextContent, Does.Contain("meal-for-people"));
    }
}
