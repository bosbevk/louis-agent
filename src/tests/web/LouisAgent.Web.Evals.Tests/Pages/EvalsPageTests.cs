using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using LouisAgent.Evals.Contracts.Api;
using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Evals.Contracts.Runs.Json;
using LouisAgent.Web.Api;
using LouisAgent.Web.Components.Admin;
using LouisAgent.Web.Docs;
using LouisAgent.Web.Evals.Client;
using LouisAgent.Web.Evals.Pages;
using LouisAgent.Web.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Evals.Tests.Pages;

/// <summary>
/// The evals' page: one card an eval, with its agent, its conversations and how its last run went; the one card that
/// says where a file goes when there is none; and the frame's key box when the key is refused.
/// </summary>
public class EvalsPageTests
{
    private static readonly DateTimeOffset Started = new(2026, 11, 23, 6, 19, 0, TimeSpan.Zero);

    private static readonly RunRow Finished = new("20261123-061900-guide", RunKind.Live, RunStatus.Finished, 3,
        Started, Started.AddSeconds(33), 5, 7);

    /// <summary>Two evals draw two cards, each with its name, its agent and its count of conversations.</summary>
    [Test]
    public void TwoEvals_DrawTwoCards_EachWithItsNameAgentAndConversations()
    {
        using var ctx = new BunitContext();
        var page = Render(ctx, Evals(new EvalRow("guide", "louis-agent-guide", 7, Finished),
            new EvalRow("kitchen", "wheatfield-kitchen", 1, null)));

        var cards = page.FindAll(".eval-card");

        Assert.That(cards, Has.Count.EqualTo(2));
        Assert.That(cards[0].TextContent, Does.Contain("guide").And.Contain("louis-agent-guide")
            .And.Contain("7 conversations"));
        Assert.That(cards[1].TextContent, Does.Contain("1 conversation").And.Not.Contain("1 conversations"));
    }

    /// <summary>A finished last run shows passed of conversations, its kind, its repeats and when it started.</summary>
    [Test]
    public void AFinishedLastRun_ShowsPassedOfConversations_ItsKindRepeatsAndWhen()
    {
        using var ctx = new BunitContext();
        var page = Render(ctx, Evals(new EvalRow("guide", "louis-agent-guide", 7, Finished)));

        string card = page.Find(".eval-card").TextContent;

        Assert.That(page.Find(".eval-card-passed").TextContent.Trim(), Is.EqualTo("5"));
        Assert.That(card, Does.Contain("of 7 passed").And.Contain("live").And.Contain("3 repeats")
            .And.Contain(Started.ToLocalTime().ToString("d MMM yyyy, HH:mm", CultureInfo.InvariantCulture)));
    }

    /// <summary>An eval never run says so, and has no link.</summary>
    [Test]
    public void AnEvalNeverRun_SaysNotRunYet_AndHasNoLink()
    {
        using var ctx = new BunitContext();
        var page = Render(ctx, Evals(new EvalRow("attorneys", "koringland-attorneys", 3, null)));

        Assert.That(page.Find(".eval-card").TextContent, Does.Contain("Not run yet."));
        Assert.That(page.Find(".eval-card").QuerySelectorAll("a"), Is.Empty);
    }

    /// <summary>A last run that could not finish says so, with how many conversations were graded.</summary>
    [Test]
    public void ALastRunThatCouldNotFinish_SaysSo_WithHowManyWereGraded()
    {
        using var ctx = new BunitContext();
        var stopped = Finished with
        {
            Status = RunStatus.CouldNotFinish,
            Passed = 1,
            Conversations = 2
        };
        var page = Render(ctx, Evals(new EvalRow("kitchen", "wheatfield-kitchen", 4, stopped)));

        Assert.That(page.Find(".eval-card-stopped").TextContent,
            Does.Contain("Could not finish").And.Contain("2 of 4 graded"));
    }

    /// <summary>A card's link is the last run's own address.</summary>
    [Test]
    public void ACardsLink_IsTheLastRunsAddress()
    {
        using var ctx = new BunitContext();
        var page = Render(ctx, Evals(new EvalRow("guide", "louis-agent-guide", 7, Finished)));

        Assert.That(page.Find(".eval-card a").GetAttribute("href"),
            Is.EqualTo(EvalAddress.RunPageOf("guide", Finished.Id)));
    }

    /// <summary>With no eval, one card says where a file goes, and links the guide through the site's docs.</summary>
    [Test]
    public void NoEval_DrawsOneCard_SayingWhereAFileGoes_AndLinkingTheGuide()
    {
        using var ctx = new BunitContext();
        var docs = new DocsAddress("https://docs.example.test");
        ctx.Services.AddSingleton(docs);
        var page = Render(ctx, Evals());

        var none = page.Find(".evals-none");
        Assert.That(none.TextContent, Does.Contain("agents/{identity}/{name}.eval.json"));
        Assert.That(none.QuerySelector("a")!.GetAttribute("href"),
            Is.EqualTo(docs.For("src/docs/guides/run-an-eval.md")));
    }

    /// <summary>A site that names no docs gets the guide named in words, with no link.</summary>
    [Test]
    public void NoEval_OnASiteWithNoDocs_NamesTheGuide_WithNoLink()
    {
        using var ctx = new BunitContext();
        var page = Render(ctx, Evals());

        var none = page.Find(".evals-none");
        Assert.That(none.TextContent, Does.Contain("Run an eval"));
        Assert.That(none.QuerySelectorAll("a"), Is.Empty);
    }

    /// <summary>A refused key shows the frame's key box.</summary>
    [TestCase(HttpStatusCode.Unauthorized)]
    [TestCase(HttpStatusCode.Forbidden)]
    public void ARefusedKey_ShowsTheKeyBox(HttpStatusCode status)
    {
        using var ctx = new BunitContext();
        var page = Render(ctx, _ => new HttpResponseMessage(status));

        Assert.That(page.FindAll(".admin-gate input[type=password]"), Has.Count.EqualTo(1));
    }

    /// <summary>Reload reads the list again.</summary>
    [Test]
    public void Reload_ReadsTheListAgain()
    {
        using var ctx = new BunitContext();
        int reads = 0;
        var page = Render(ctx, request =>
        {
            reads++;
            return Evals()(request);
        });

        page.Find("button.evals-reload").Click();

        page.WaitForAssertion(() => Assert.That(reads, Is.EqualTo(2)));
    }

    /// <summary>A stub of the API that answers the list with these rows.</summary>
    private static Func<HttpRequestMessage, HttpResponseMessage> Evals(params EvalRow[] rows) => _ =>
        new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new EvalsReply(rows), options: RunJson.Options)
        };

    /// <summary>The page over a stub of the API, rendered until it has loaded.</summary>
    private static IRenderedComponent<EvalsPage> Render(BunitContext ctx,
        Func<HttpRequestMessage, HttpResponseMessage> answer)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var settings = new ChatSettings();
        var api = new AgentApi(new HttpClient(new StubHandler(answer)) { BaseAddress = new Uri("http://localhost/") },
            settings);
        ctx.Services.AddSingleton(new ChatStore(ctx.JSInterop.JSRuntime, settings));
        ctx.Services.AddSingleton(api);
        ctx.Services.AddSingleton(new EvalsApi(api));
        ctx.Services.AddSingleton(new AdminAccess());

        var page = ctx.Render<EvalsPage>();
        page.WaitForState(() => page.FindAll(".evals-loading").Count == 0, TimeSpan.FromSeconds(10));
        return page;
    }
}
