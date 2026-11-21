using LouisAgent.Api.Contracts.Admin.Evals;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Choices;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Web.Evals.Admin.Starting;
using LouisAgent.Web.Evals.Components.EvalsPage.Details;
using LouisAgent.Web.Api;
using LouisAgent.Web.Chats;
using LouisAgent.Web.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Evals.Tests.Components.EvalsPage.Details;

/// <summary>
/// An eval's details as a person sees them (CL26's card, F53.2's details pane): the latest run up top with a ring
/// per mode, the runs before it as a timeline where a replay links to its recording, and every grader in the fold.
/// </summary>
public class EvalDetailsTests
{
    /// <summary>When the runs started; the card only shows it.</summary>
    private static readonly DateTimeOffset T0 = new(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);

    /// <summary>A finished run of two modes, replaying another run if it says so.</summary>
    private static EvalRunRow Run(string id, string? replayOf = null) =>
        new(new StoredRunRow(id, "cook", 1, "done", 0, T0, T0.AddMinutes(9), new RunTotalRow(0.07m, "USD", 1000, 90),
                null, null, "cook", 10, null, null, null, replayOf),
            [
                new ModeRow(SampleMode.Alpha, 1, [], null, null, 0.25, 0.25, 1, 0, 0),
                new ModeRow(SampleMode.Beta, 1, [], null, null, 1.0, 1.0, 1, 0, 0)
            ]);

    /// <summary>An eval with three graders and these runs, newest first.</summary>
    private static EvalRow Eval(IReadOnlyList<EvalRunRow> runs) =>
        new("cook", SampleIdentity.Alpha, 10, "Meals", true, new EvalDatasetRow("meals", 7), ["budget", "follow-up"],
        [
            new("no-error", 1, CheckKind.FixedRule.Wire()),
            new("meal", 2, CheckKind.Model.Wire()),
            new("meal.human", 2, CheckKind.Human.Wire())
        ], [SampleMode.Alpha, SampleMode.Beta], [], runs.Count == 0, runs);

    /// <summary>The details of an eval with these runs, over an API that has no trend for it.</summary>
    private static IRenderedComponent<EvalDetails> Render(BunitContext ctx, IReadOnlyList<EvalRunRow> runs,
        Action<StartRequest>? onRun = null, bool canRun = true)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(new AgentApi(
            new HttpClient(new StubHandler(_ => new HttpResponseMessage(System.Net.HttpStatusCode.NotFound)
            {
                Content = new StringContent("""{"type":"error","error":{"type":"not_found","message":"no"}}""",
                    System.Text.Encoding.UTF8, "application/json")
            }))
            { BaseAddress = new Uri("http://localhost/") }, new ChatSettings()));
        return ctx.Render<EvalDetails>(p => p.Add(x => x.Eval, Eval(runs)).Add(x => x.CanRun, canRun)
            .Add(x => x.OnRun, onRun ?? (_ => { })));
    }

    /// <summary>The latest run's scorecard has a ring per mode, each with its pass@k in the middle.</summary>
    [Test]
    public void TheScorecard_HasARingPerMode()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx, [Run("run_2"), Run("run_1")]);

        var rings = cut.FindAll(".eval-score .donut-centre strong").Select(s => s.TextContent).ToList();
        Assert.That(rings, Is.EqualTo(new[] { "25%", "100%" }));
    }

    /// <summary>A replay on the timeline is tagged and links to the recording it replayed.</summary>
    [Test]
    public void AReplayOnTheTimeline_LinksToItsRecording()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx, [Run("run_3"), Run("run_2", replayOf: "run_1"), Run("run_1")]);

        var tag = cut.Find(".eval-timeline .run-origin.is-replay");
        Assert.That(tag.TextContent, Is.EqualTo("replay of run_1"));
        Assert.That(tag.GetAttribute("href"), Is.EqualTo("admin/runs/run_1"));
        Assert.That(cut.FindAll(".eval-timeline > li"), Has.Count.EqualTo(2), "the latest is in the scorecard");
    }

    /// <summary>The fold holds every grader, by kind, with the categories and modes.</summary>
    [Test]
    public void TheFold_HoldsEveryGrader()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx, [Run("run_1")]);

        var graders = cut.FindAll(".eval-checks .eval-grader").Select(g => g.TextContent.Trim()).ToList();
        Assert.That(graders, Is.EqualTo(new[] { "no-error v1", "meal v2", "meal.human v2" }));
        Assert.That(cut.Find(".eval-checks .fold-note").TextContent,
            Is.EqualTo("3 graders · 2 categories · 2 modes"));
    }

    /// <summary>An eval that never ran has no scorecard and no timeline, but still says what it checks.</summary>
    [Test]
    public void AnEvalThatNeverRan_ShowsOnlyWhatItChecks()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx, []);

        Assert.That(cut.FindAll(".eval-score"), Is.Empty);
        Assert.That(cut.FindAll(".eval-timeline"), Is.Empty);
        Assert.That(cut.FindAll(".eval-checks"), Has.Count.EqualTo(1));
    }

    /// <summary>
    /// The Run button asks for a run of this eval at the repeats chosen (F53.2 step 3: what the check's card did).
    /// </summary>
    [Test]
    public void TheRunButton_AsksForARun_AtTheRepeatsChosen()
    {
        using var ctx = new BunitContext();
        StartRequest? asked = null;
        var cut = Render(ctx, [Run("run_1")], request => asked = request);

        cut.FindAll(".run-controls .segmented button").First(b => b.TextContent == "3").Click();
        cut.Find(".run-controls .run-button").Click();

        Assert.That(asked, Is.EqualTo(new StartRequest("cook", 3)));
    }

    /// <summary>When the store doesn't answer no run may start: the Run button is off.</summary>
    [Test]
    public void WithoutTheStore_TheRunButtonIsOff()
    {
        using var ctx = new BunitContext();
        var cut = Render(ctx, [Run("run_1")], canRun: false);

        Assert.That(cut.Find(".run-controls .run-button").HasAttribute("disabled"), Is.True);
    }

    /// <summary>The runs table in the details holds every run of the eval, newest first, and names the eval.</summary>
    [Test]
    public void TheRunsTable_HoldsEveryRun()
    {
        using var ctx = new BunitContext();
        var cut = Render(ctx, [Run("run_2"), Run("run_1")]);

        Assert.That(cut.Find(".runs-list strong").TextContent, Is.EqualTo("Runs · cook"));
        Assert.That(cut.FindAll(".runs-list .runs > li"), Has.Count.EqualTo(2));
    }
}
