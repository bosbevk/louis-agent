using System.Net;
using System.Net.Http.Json;
using LouisAgent.Evals.Contracts.Api;
using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Evals.Contracts.Runs.Json;
using LouisAgent.Web.Api;
using LouisAgent.Web.Components.Admin;
using LouisAgent.Web.Evals.Admin;
using LouisAgent.Web.Evals.Client;
using LouisAgent.Web.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Evals.Tests.Admin;

/// <summary>
/// The evals' card on the admin's home: how many evals the host has, and how many of their last runs passed every
/// conversation, read from the same list the page reads.
/// </summary>
public class EvalsCardTests
{
    private static readonly DateTimeOffset Started = new(2026, 11, 23, 6, 19, 0, TimeSpan.Zero);

    /// <summary>The card counts the evals and the last runs that passed in full.</summary>
    [Test]
    public void TheCard_CountsTheEvals_AndTheLastRunsThatPassedEveryConversation()
    {
        using var ctx = new BunitContext();
        var card = Render(ctx, new EvalRow("guide", "louis-agent-guide", 2, Run(2, 2)),
            new EvalRow("kitchen", "wheatfield-kitchen", 4, Run(3, 4)),
            new EvalRow("attorneys", "koringland-attorneys", 3, null));

        card.WaitForAssertion(() =>
        {
            Assert.That(card.Find(".evals-card-count").TextContent, Does.Contain("3"));
            Assert.That(card.Find(".evals-card-passed").TextContent, Does.Contain("1"));
        });
    }

    /// <summary>With no eval, the card says so.</summary>
    [Test]
    public void WithNoEval_TheCardSaysSo()
    {
        using var ctx = new BunitContext();
        var card = Render(ctx);

        card.WaitForAssertion(() => Assert.That(card.Find(".evals-card-none").TextContent, Does.Contain("No evals")));
    }

    /// <summary>The card opens the evals' page.</summary>
    [Test]
    public void TheCard_OpensTheEvalsPage()
    {
        using var ctx = new BunitContext();
        var card = Render(ctx);

        Assert.That(card.Find("a.admin-card-open").GetAttribute("href"), Is.EqualTo(EvalAddress.EvalsPage));
    }

    /// <summary>A finished last run of this many conversations, this many passed.</summary>
    private static RunRow Run(int passed, int conversations) =>
        new("20261123-061900-guide", RunKind.Live, RunStatus.Finished, 1, Started, Started.AddSeconds(30), passed,
            conversations);

    /// <summary>The card over a stub of the API that answers the list with these rows.</summary>
    private static IRenderedComponent<EvalsCard> Render(BunitContext ctx, params EvalRow[] rows)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var settings = new ChatSettings();
        var api = new AgentApi(
            new HttpClient(new StubHandler(_ =>
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new EvalsReply(rows), options: RunJson.Options)
                }))
            { BaseAddress = new Uri("http://localhost/") }, settings);
        ctx.Services.AddSingleton(new ChatStore(ctx.JSInterop.JSRuntime, settings));
        ctx.Services.AddSingleton(api);
        ctx.Services.AddSingleton(new EvalsApi(api));
        ctx.Services.AddSingleton(new AdminAccess());
        return ctx.Render<EvalsCard>();
    }
}
