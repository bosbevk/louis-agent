using LouisAgent.Testing;
using System.Text.Json;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Puls.Admin.Explorer.Place;
using LouisAgent.Puls.Admin.Notes;
using LouisAgent.Puls.Admin.Reviews;
using LouisAgent.Puls.Components.Admin;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Puls.Tests.Components.Admin;

/// <summary>
/// The explorer's place comes from the page and goes back to it (F53.1 step 3): a place handed in opens that view
/// at once, every move is reported so the page can put it in the address, and a place handed in later (Back, or a
/// link) moves the explorer there; the repeat picker keeps the open conversation when the other repeat has it.
/// </summary>
public class RunExplorerPlaceTests
{
    /// <summary>One mode's JSON for a repeat.</summary>
    private static string ModeJson(int repeat) =>
        $$"""{"name":"puls-cook","repeat":{{repeat}},"toolsets":["{{SampleToolset.Alpha}}"]}""";

    /// <summary>One conversation's JSON for a repeat and a task.</summary>
    private static string ConversationJson(int repeat, string task) =>
        $$"""{"mode":"puls-cook","repeat":{{repeat}},"task":"{{task}}","role":null,"errors":0}""";

    /// <summary>A run with this many repeats, each with two conversations.</summary>
    private static string Run(int repeats) => SampleScenario.Fill($$"""
        {"run":{"id":"run1","check":"puls-cook","repeat":{{repeats}},"status":"done","started_at":"2026-10-26T19:00:00+00:00"},
         "modes":[{{string.Join(",", Enumerable.Range(1, repeats).Select(ModeJson))}}],
         "conversations":[{{string.Join(",", Enumerable.Range(1, repeats).SelectMany(r => new[] { "<ScenarioAlpha>", "<ScenarioBeta>" }.Select(t => ConversationJson(r, t))))}}],
         "turns":[],"requests":[]}
        """);

    /// <summary>Renders the explorer on a run at a place, collecting every move it reports.</summary>
    private static (IRenderedComponent<RunExplorer> Cut, List<ExplorerPlace> Moves) Render(ExplorerPlace? place,
        int repeats = 1)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        // What the explorer needs: an API that answers nothing, a reviewer, and the notes state.
        ctx.Services.AddSingleton(new AgentApi(
            new HttpClient(new StubHandler(_ => new HttpResponseMessage()))
            {
                BaseAddress = new Uri("http://localhost/")
            }, new ChatSettings()));
        ctx.Services.AddSingleton(new ReviewerState(ctx.JSInterop.JSRuntime));
        ctx.Services.AddSingleton(new RunNotesState());

        // The run as the page reads it from the API: the contract's stored run reply.
        var run = JsonSerializer.Deserialize<StoredRunReply>(Run(repeats), AdminJson.SnakeCase)!;
        var moves = new List<ExplorerPlace>();
        var cut = ctx.Render<RunExplorer>(p => p.Add(x => x.Run, run).Add(x => x.Place, place)
            .Add(x => x.OnMoved, EventCallback.Factory.Create<ExplorerPlace>(ctx, moves.Add)));
        return (cut, moves);
    }

    /// <summary>A place handed in opens its view at once: a conversation's place shows that conversation.</summary>
    [Test]
    public void APlaceHandedIn_OpensItsViewAtOnce() =>
        Assert.That(Render(ExplorerPlace.Conversation(SampleScenario.Beta)).Cut.Find(".crumb.here").TextContent,
            Is.EqualTo(SampleScenario.Beta));

    /// <summary>With no place, the explorer stays at the run and reports nothing.</summary>
    [Test]
    public void NoPlace_StaysAtTheRun_AndReportsNothing()
    {
        var (cut, moves) = Render(null);

        Assert.That(cut.Find(".crumb.here").TextContent, Is.EqualTo("Run"));
        Assert.That(moves, Is.Empty);
    }

    /// <summary>Every move is reported: opening the conversations says so, and going back to the run too.</summary>
    [Test]
    public void EveryMove_IsReported()
    {
        var (cut, moves) = Render(ExplorerPlace.Start);

        cut.Find(".tile").Click();
        cut.FindAll(".crumb").First().Click();

        Assert.That(moves, Is.EqualTo(new[] { new ExplorerPlace(ExplorerView.Conversations), ExplorerPlace.Start }));
    }

    /// <summary>The filter to the conversations still to review is part of the place.</summary>
    [Test]
    public void TheFilterToReview_IsPartOfThePlace()
    {
        var (cut, moves) = Render(new ExplorerPlace(ExplorerView.Conversations));

        cut.Find("[data-filter=to-review]").Click();

        Assert.That(moves.Last(), Is.EqualTo(new ExplorerPlace(ExplorerView.Conversations, Review: true)));
    }

    /// <summary>A place handed in later moves the explorer there, as Back or a link does, without a report.</summary>
    [Test]
    public void APlaceHandedInLater_MovesTheExplorer_WithoutAReport()
    {
        var (cut, moves) = Render(ExplorerPlace.Conversation(SampleScenario.Beta));

        cut.Render(p => p.Add(x => x.Place, ExplorerPlace.Start));

        Assert.That(cut.Find(".crumb.here").TextContent, Is.EqualTo("Run"));
        Assert.That(moves, Is.Empty);
    }

    /// <summary>
    /// Changing the repeat keeps the conversation you are in when the other repeat has it too, and falls back to the
    /// overview only when it doesn't.
    /// </summary>
    [Test]
    public void ChangingTheRepeat_KeepsTheOpenConversation()
    {
        var (cut, _) = Render(ExplorerPlace.Conversation(SampleScenario.Beta), repeats: 2);

        cut.FindAll(".segmented button").First(b => b.TextContent == "2").Click();

        Assert.That(cut.Find(".crumb.here").TextContent, Is.EqualTo(SampleScenario.Beta));
        Assert.That(cut.Find(".segmented button.active").TextContent, Is.EqualTo("2"));
    }
}
