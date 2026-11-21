using System.Net;
using System.Text;
using LouisAgent.Web.Evals.Admin.Reviews;
using LouisAgent.Web.Evals.Admin.Reviews.Queue;
using LouisAgent.Web.Evals.Components.Reviews.Queue;
using LouisAgent.Testing;
using LouisAgent.Web.Api;
using LouisAgent.Web.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Evals.Tests.Components.Reviews.Queue;

/// <summary>
/// The count beside Review in the sidebar (F53.3 step 3): read once from the queue for the kept name, shown while
/// something waits, and following the shared state when the Review page changes it.
/// </summary>
public class ReviewCountBadgeTests
{
    /// <summary>The badge reads the queue once, for the kept name, and shows the total.</summary>
    [Test]
    public void ItReadsTheQueueOnce_ForTheKeptName_AndShowsTheTotal()
    {
        using var ctx = new BunitContext();
        var (cut, handler, _) = Render(ctx, """{"items":[],"total":7}""");

        cut.WaitForState(() => cut.FindAll(".review-count").Count == 1, TimeSpan.FromSeconds(10));
        Assert.That(cut.Find(".review-count").TextContent, Is.EqualTo("7"));
        Assert.That(handler.Requests.Single().Url,
            Does.EndWith("/v1/admin/evals/reviews/queue?reviewer=Louis&limit=1"));
    }

    /// <summary>With nothing waiting there is no badge at all.</summary>
    [Test]
    public void WithNothingWaiting_ThereIsNoBadge()
    {
        using var ctx = new BunitContext();
        var (cut, _, state) = Render(ctx, """{"items":[],"total":0}""");

        cut.WaitForState(() => state.Count == 0, TimeSpan.FromSeconds(10));
        Assert.That(cut.FindAll(".review-count"), Is.Empty);
    }

    /// <summary>The badge follows the shared count: the Review page sets it after a grade.</summary>
    [Test]
    public void ItFollowsTheSharedCount()
    {
        using var ctx = new BunitContext();
        var (cut, _, state) = Render(ctx, """{"items":[],"total":7}""");
        cut.WaitForState(() => cut.FindAll(".review-count").Count == 1, TimeSpan.FromSeconds(10));

        state.Set(6);

        cut.WaitForState(() => cut.Find(".review-count").TextContent == "6", TimeSpan.FromSeconds(10));
    }

    /// <summary>The badge as Louis, over an API that answers the queue with this JSON.</summary>
    private static (IRenderedComponent<ReviewCountBadge> Cut, StubHandler Handler, ReviewQueueState State) Render(
        BunitContext ctx, string queue)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.JSInterop.Setup<string?>("localStorage.getItem", ReviewerState.StorageKey).SetResult("Louis");
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(queue, Encoding.UTF8, "application/json")
        });
        ctx.Services.AddSingleton(new AgentApi(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") },
            new ChatSettings()));
        ctx.Services.AddSingleton(new ReviewerState(ctx.JSInterop.JSRuntime));
        var state = new ReviewQueueState();
        ctx.Services.AddSingleton(state);
        return (ctx.Render<ReviewCountBadge>(), handler, state);
    }
}
