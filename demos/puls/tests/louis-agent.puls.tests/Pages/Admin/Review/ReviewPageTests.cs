using System.Net;
using System.Text;
using LouisAgent.Puls.Admin.Reviews;
using LouisAgent.Puls.Admin.Reviews.Queue;
using LouisAgent.Testing;
using LouisAgent.Web.Api;
using LouisAgent.Web.Chats;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Testing;
using ReviewPage = LouisAgent.Puls.Pages.Admin.Review;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Puls.Tests.Pages.Admin.Review;

/// <summary>
/// Pins the Review page (F53.3 step 2): one answer at a time from the queue the API keeps for this reviewer, with
/// where it is from and the review panel; a grade opens the next, Skip moves on, and with nothing waiting the page
/// says so (golden: <c>Review.html</c>).
/// </summary>
public class ReviewPageTests
{
    /// <summary>
    /// Two answers waiting: bobotie from the newer run, sosaties from the older; the file beside this test.
    /// </summary>
    private static readonly string Two = SampleEval.Fill(SampleMode.Fill(File.ReadAllText(Path.Combine(
        TestPaths.RepoRoot, "demos/puls/tests/louis-agent.puls.tests", "Pages", "Admin", "Review", "Review.queue.json"))));

    /// <summary>Nothing waits.</summary>
    private const string Empty = """{"items":[],"total":0}""";

    /// <summary>The first item is open: its eval, run, mode and task, its place in the queue, and the panel.</summary>
    [Test]
    public void TheFirstAnswer_IsOpen_WithWhereItIsFrom_AndThePanel()
    {
        using var ctx = new BunitContext();
        var (cut, _) = Render(ctx, Two);

        cut.WaitForState(() => cut.FindAll(".review-item").Count == 1, TimeSpan.FromSeconds(10));
        Assert.That(cut.Find(".review-item-eval").TextContent, Is.EqualTo(SampleEval.Beta));
        Assert.That(cut.Find(".review-item-run").GetAttribute("href"), Is.EqualTo("admin/runs/run_2"));
        Assert.That(cut.Find(".review-item-task").TextContent, Is.EqualTo("bobotie"));
        Assert.That(cut.Find(".review-place").TextContent, Is.EqualTo("1 of 2"));
        Assert.That(cut.Find(".review-panel .review-answer").TextContent, Does.Contain("Bobotie vir ses"));
        Snapshots.Match("Pages/Admin/Review/Review.html", cut.Markup);
    }

    /// <summary>A grade given opens the next answer, and the head counts it.</summary>
    [Test]
    public void AGrade_OpensTheNextAnswer()
    {
        using var ctx = new BunitContext();
        var (cut, handler) = Render(ctx, Two);
        cut.WaitForState(() => cut.FindAll(".review-pass").Count == 1, TimeSpan.FromSeconds(10));

        cut.Find(".review-pass").Click();

        cut.WaitForState(() => cut.Find(".review-item-task").TextContent == "sosaties", TimeSpan.FromSeconds(10));
        Assert.That(handler.Requests.Count(r => r.Method == HttpMethod.Put), Is.EqualTo(1));
        Assert.That(cut.Find(".review-place").TextContent, Is.EqualTo("2 of 2"));
        Assert.That(cut.Find(".review-progress").TextContent, Is.EqualTo("1 graded · 1 left"));
    }

    /// <summary>Skip moves on without a grade.</summary>
    [Test]
    public void Skip_MovesOn_WithoutAGrade()
    {
        using var ctx = new BunitContext();
        var (cut, handler) = Render(ctx, Two);
        cut.WaitForState(() => cut.FindAll(".review-skip").Count == 1, TimeSpan.FromSeconds(10));

        cut.Find(".review-skip").Click();

        cut.WaitForState(() => cut.Find(".review-item-task").TextContent == "sosaties", TimeSpan.FromSeconds(10));
        Assert.That(handler.Requests.Any(r => r.Method == HttpMethod.Put), Is.False);
        Assert.That(cut.Find(".review-progress").TextContent, Is.EqualTo("0 graded · 2 left"));
    }

    /// <summary>The keys: 1 grades pass without a click, and the next answer opens (F53.3 step 3).</summary>
    [Test]
    public void PressingOne_GradesPass_AndOpensTheNext()
    {
        using var ctx = new BunitContext();
        var (cut, handler) = Render(ctx, Two);
        cut.WaitForState(() => cut.FindAll(".review-pass").Count == 1, TimeSpan.FromSeconds(10));

        cut.Find(".review-page").KeyDown("1");

        cut.WaitForState(() => cut.Find(".review-item-task").TextContent == "sosaties", TimeSpan.FromSeconds(10));
        var put = handler.Requests.Single(r => r.Method == HttpMethod.Put);
        Assert.That(put.Body, Does.Contain("\"verdict\":\"pass\""));
    }

    /// <summary>A key typed in the note stays a letter in the note: it never grades.</summary>
    [Test]
    public void AKeyInTheNote_DoesNotGrade()
    {
        using var ctx = new BunitContext();
        var (cut, handler) = Render(ctx, Two);
        cut.WaitForState(() => cut.FindAll(".review-note").Count == 1, TimeSpan.FromSeconds(10));

        // The note stops the key (stopPropagation): bUnit reports that no handler took it, which is the point.
        Assert.Throws<MissingEventHandlerException>(() => cut.Find(".review-note").KeyDown("1"));

        Assert.That(cut.Find(".review-item-task").TextContent, Is.EqualTo("bobotie"));
        Assert.That(handler.Requests.Any(r => r.Method == HttpMethod.Put), Is.False);
    }

    /// <summary>The shared count the sidebar's badge shows falls by one after a grade (F53.3 step 3).</summary>
    [Test]
    public void AGrade_LowersTheSharedCount()
    {
        using var ctx = new BunitContext();
        var (cut, _) = Render(ctx, Two);
        var state = ctx.Services.GetRequiredService<ReviewQueueState>();
        cut.WaitForState(() => state.Count == 2, TimeSpan.FromSeconds(10));

        cut.Find(".review-pass").Click();

        cut.WaitForState(() => state.Count == 1, TimeSpan.FromSeconds(10));
    }

    /// <summary>With nothing waiting the page says so, by name.</summary>
    [Test]
    public void WithNothingWaiting_ThePageSaysSo()
    {
        using var ctx = new BunitContext();
        var (cut, _) = Render(ctx, Empty);

        cut.WaitForState(() => cut.FindAll(".review-empty").Count == 1, TimeSpan.FromSeconds(10));
        Assert.That(cut.Find(".review-empty").TextContent, Does.Contain("Nothing waits for your grade, Louis"));
    }

    /// <summary>The queue is read for the kept name.</summary>
    [Test]
    public void TheQueue_IsReadForTheKeptName()
    {
        using var ctx = new BunitContext();
        var (cut, handler) = Render(ctx, Two);

        cut.WaitForState(() => cut.FindAll(".review-item").Count == 1, TimeSpan.FromSeconds(10));
        Assert.That(handler.Requests.Single(r => r.Method == HttpMethod.Get).Url,
            Does.EndWith("/v1/admin/evals/reviews/queue?reviewer=Louis&limit=20"));
    }

    /// <summary>The page, as Louis, over an API that serves this queue and takes any grade.</summary>
    private static (IRenderedComponent<ReviewPage> Cut, StubHandler Handler) Render(BunitContext ctx, string queue)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.JSInterop.Setup<string?>("localStorage.getItem", ReviewerState.StorageKey).SetResult("Louis");
        var handler = new StubHandler(request => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(request.Method == HttpMethod.Put ? """{"verdict":"pass"}""" : queue,
                Encoding.UTF8, "application/json")
        });

        var settings = new ChatSettings();
        var api = new AgentApi(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }, settings);
        ctx.Services.AddSingleton(settings);
        ctx.Services.AddSingleton(api);
        ctx.Services.AddSingleton(new ChatStore(ctx.JSInterop.JSRuntime, settings));
        ctx.Services.AddSingleton(new ReviewerState(ctx.JSInterop.JSRuntime));
        ctx.Services.AddSingleton(new ReviewQueueState());
        return (ctx.Render<ReviewPage>(), handler);
    }
}
