using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bunit.TestDoubles;
using LouisAgent.Api.Contracts;
using LouisAgent.Evals.Contracts.Api;
using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Evals.Contracts.Runs.Json;
using LouisAgent.Web.Api;
using LouisAgent.Web.Evals.Client;
using LouisAgent.Web.Evals.Components.Evals;
using LouisAgent.Web.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Evals.Tests.Components.Evals;

/// <summary>
/// F126.3.4: an eval's card starts a run. A replay is free and starts at once; a live run spends money, so it asks
/// first, inline on the card, with what it is likely to cost. After a start the browser goes to the run's page.
/// </summary>
public class EvalCardStartTests
{
    private const string RunId = "20261123-131900-guide";

    private static readonly DateTimeOffset Started = new(2026, 11, 23, 13, 19, 0, TimeSpan.Zero);

    /// <summary>A card with a live run behind it offers a free replay and a live run.</summary>
    [Test]
    public void ACardWithALiveRun_OffersReplayFree_AndRunLive()
    {
        using var ctx = new BunitContext();
        var card = Render(ctx, Guide(LastLive(0.05m)), Accepting());

        Assert.That(card.FindAll("button").Select(b => b.TextContent.Trim()),
            Is.EquivalentTo(new[] { "Replay free", "Run live…" }));
    }

    /// <summary>A card with no live run offers only a live run, and says why there is nothing to replay.</summary>
    [Test]
    public void ACardWithNoLiveRun_OffersOnlyRunLive_AndSaysWhy()
    {
        using var ctx = new BunitContext();
        var card = Render(ctx, Guide(null), Accepting());

        Assert.That(card.FindAll("button").Select(b => b.TextContent.Trim()), Is.EqualTo(new[] { "Run live…" }));
        Assert.That(card.Markup, Does.Contain("Nothing to replay yet. A replay plays a live run back."));
    }

    /// <summary>Replay free starts a replay and goes to the run's page.</summary>
    [Test]
    public void ReplayFree_StartsAReplay_AndGoesToTheRunsPage()
    {
        using var ctx = new BunitContext();
        var stub = Accepting();
        var card = Render(ctx, Guide(LastLive(0.05m)), stub);

        card.FindAll("button").Single(b => b.TextContent.Contains("Replay free")).Click();

        card.WaitForAssertion(() => Assert.That(ctx.Services.GetRequiredService<NavigationManager>().Uri,
            Does.EndWith(EvalAddress.RunPageOf("guide", RunId))));
        Assert.That(Asked(stub).Kind, Is.EqualTo(RunKind.Replay));
    }

    /// <summary>Run live asks to open the ask and starts nothing.</summary>
    [Test]
    public void RunLive_AsksToOpenTheAsk_AndStartsNothing()
    {
        using var ctx = new BunitContext();
        var stub = Accepting();
        string? asked = null;
        var card = Render(ctx, Guide(LastLive(0.05m)), stub, onAsk: name => asked = name);

        card.FindAll("button").Single(b => b.TextContent.Contains("Run live")).Click();

        Assert.That(asked, Is.EqualTo("guide"));
        Assert.That(stub.Requests, Is.Empty);
    }

    /// <summary>
    /// The ask names the eval and its conversations, offers one repeat or three with three chosen, and says the
    /// estimate for the repeats chosen; choosing one changes the estimate and the button.
    /// </summary>
    [Test]
    public void TheAsk_SaysTheEstimateForTheRepeatsChosen()
    {
        using var ctx = new BunitContext();
        var card = Render(ctx, Guide(LastLive(0.05m)), Accepting(), askOpen: true);

        string three = card.Find(".run-ask").TextContent;
        card.FindAll(".run-ask-repeats button").Single(b => b.TextContent.Trim() == "1").Click();
        string one = card.Find(".run-ask").TextContent;

        Assert.That(three, Does.Contain("Run guide live?").And.Contain("7 conversations")
            .And.Contain("About $0.15 for 3 repeats, from what the last live run cost.")
            .And.Contain("Run live, about $0.15"));
        Assert.That(one, Does.Contain("About $0.05 for 1 repeat").And.Contain("Run live, about $0.05"));
    }

    /// <summary>With no estimate the ask says the cost isn't known yet, and its button names no figure.</summary>
    [Test]
    public void WithNoEstimate_TheAskSaysTheCostIsNotKnown()
    {
        using var ctx = new BunitContext();
        var card = Render(ctx, Guide(null), Accepting(), askOpen: true);

        Assert.That(card.Find(".run-ask").TextContent,
            Does.Contain("The cost isn't known yet. No live run with a price has finished."));
        Assert.That(card.Find(".run-ask-go").TextContent.Trim(), Is.EqualTo("Run live"));
    }

    /// <summary>The ask's button starts a live run of the repeats chosen; Don't run closes it, starting nothing.</summary>
    [Test]
    public void TheAsksButton_StartsALiveRun_AndDontRun_ClosesIt()
    {
        using var ctx = new BunitContext();
        var stub = Accepting();
        string? asked = "guide";
        var card = Render(ctx, Guide(LastLive(0.05m)), stub, askOpen: true, onAsk: name => asked = name);

        card.Find(".run-ask-cancel").Click();
        Assert.That((asked, stub.Requests.Count), Is.EqualTo(((string?)null, 0)));

        card.FindAll(".run-ask-repeats button").Single(b => b.TextContent.Trim() == "1").Click();
        card.Find(".run-ask-go").Click();
        card.WaitForAssertion(() => Assert.That(stub.Requests, Has.Count.EqualTo(1)));
        Assert.That(Asked(stub), Is.EqualTo(new StartRunRequest(RunKind.Live, 1)));
    }

    /// <summary>A start the API refuses shows the API's reason on the card, and the page stays.</summary>
    [Test]
    public void ARefusedStart_ShowsTheApisReason_OnTheCard()
    {
        using var ctx = new BunitContext();
        // The client reads only the refusal's words, so its type is any.
        var refusal = new ApiError("error", new ApiErrorDetail("a-refusal", "A run is already going."));
        var stub = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = JsonContent.Create(refusal, options: JsonSerializerOptions.Web)
        });
        var card = Render(ctx, Guide(LastLive(0.05m)), stub);

        card.FindAll("button").Single(b => b.TextContent.Contains("Replay free")).Click();

        card.WaitForAssertion(() =>
            Assert.That(card.Find(".eval-card-refused").TextContent, Does.Contain("A run is already going.")));
        Assert.That(ctx.Services.GetRequiredService<NavigationManager>().Uri, Does.Not.Contain("/runs/"));
    }

    /// <summary>While another eval's run goes, this card's buttons are off.</summary>
    [Test]
    public void WhileAnotherRunGoes_TheButtonsAreOff()
    {
        using var ctx = new BunitContext();
        var card = Render(ctx, Guide(LastLive(0.05m)), Accepting(), waiting: true);

        Assert.That(card.FindAll("button").Select(b => b.HasAttribute("disabled")), Has.All.True);
    }

    /// <summary>A card whose run is going says since when and links the run's page to follow it.</summary>
    [Test]
    public void ACardWhoseRunGoes_SaysSinceWhen_AndLinksToFollowIt()
    {
        using var ctx = new BunitContext();
        var going = Run(RunStatus.Running);
        var card = Render(ctx, Guide(LastLive(0.05m)) with { LastRun = going }, Accepting());

        Assert.That(card.Markup, Does.Contain("Going since"));
        Assert.That(card.Find("a.eval-card-follow").GetAttribute("href"),
            Is.EqualTo(EvalAddress.RunPageOf("guide", going.Id)));
        Assert.That(card.FindAll("button"), Is.Empty);
    }

    /// <summary>A card whose last run was stopped says so.</summary>
    [Test]
    public void ACardWhoseLastRunWasStopped_SaysSo() =>
        Assert.That(
            Render(new BunitContext(), Guide(LastLive(0.05m)) with { LastRun = Run(RunStatus.Stopped) }, Accepting())
                .Find(".eval-card-stopped").TextContent, Does.Contain("Stopped"));

    /// <summary>The guide's eval of seven conversations, with this last live run.</summary>
    private static EvalRow Guide(RunRow? lastLive) => new("guide", "louis-agent-guide", 7, lastLive, lastLive);

    /// <summary>A finished live run of one repeat that spent this much in all.</summary>
    private static RunRow LastLive(decimal spent) => Run(RunStatus.Finished) with
    {
        Repeats = 1,
        Cost = spent,
        Currency = "USD"
    };

    /// <summary>A run in short of the guide's eval, where it is.</summary>
    private static RunRow Run(RunStatus status) =>
        new("20261123-120000-guide", RunKind.Live, status, 3, Started, null, 5, 7);

    /// <summary>A stub of the API that accepts a start with the run's address.</summary>
    private static StubHandler Accepting() => new(_ => new HttpResponseMessage(HttpStatusCode.Accepted)
    {
        Headers = { Location = new Uri($"/v1/admin/evals/runs/{RunId}", UriKind.Relative) }
    });

    /// <summary>The start the card sent.</summary>
    private static StartRunRequest Asked(StubHandler stub) =>
        JsonSerializer.Deserialize<StartRunRequest>(stub.Requests.Single().Body!, RunJson.Options)!;

    /// <summary>The card over a stub of the API, its ask open or not, another run going or not.</summary>
    private static IRenderedComponent<EvalCard> Render(BunitContext ctx, EvalRow row, StubHandler stub,
        bool askOpen = false, bool waiting = false, Action<string?>? onAsk = null)
    {
        var api = new AgentApi(new HttpClient(stub) { BaseAddress = new Uri("http://localhost/") }, new ChatSettings());
        ctx.Services.AddSingleton(new EvalsApi(api));
        return ctx.Render<EvalCard>(parameters => parameters.Add(p => p.Row, row).Add(p => p.AskOpen, askOpen)
            .Add(p => p.Waiting, waiting).Add(p => p.OnAsk, onAsk ?? (_ => { })));
    }
}
