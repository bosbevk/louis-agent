using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using LouisAgent.Api.Contracts.Admin.Checks;
using LouisAgent.Api.Contracts.Admin.Evals;
using LouisAgent.Api.Endpoints.Admin.Evals;
using LouisAgent.Testing;
using Microsoft.AspNetCore.Http.HttpResults;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;

namespace LouisAgent.Api.Tests.Endpoints;

/// <summary>
/// The admin's eval list shows every eval the catalogue has, with what it is, and its runs once there are any: an eval
/// that never ran is listed as never run, not hidden.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public class EvalHandlersTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 25, 9, 0, 0, TimeSpan.Zero);

    /// <summary>The list says in its signature what it answers: always the list.</summary>
    [Test]
    public void TheList_DeclaresItsAnswer()
    {
        Assert.That(typeof(EvalHandlers).GetMethod("GetList")?.ReturnType,
            Is.EqualTo(typeof(Task<JsonHttpResult<EvalListReply>>)));
    }

    /// <summary>An eval with no run yet is listed with its graders, dataset and categories, and as never run.</summary>
    [Test]
    public async Task AnEvalWithNoRun_IsListed_WithItsGraders_AndNeverRun()
    {
        var list = await ListAsync(new InMemoryBenchmarkStore());

        var eval = list.Evals.Single();
        Assert.That((eval.Name, eval.Identity, eval.Version, eval.Proves, eval.Manual),
            Is.EqualTo((SampleEval.Beta, (string?)SampleIdentity.Alpha, (int?)3, "A meal", false)));
        Assert.That(eval.Graders,
            Is.EqualTo(new[] { new EvalGraderRow("priced", 1, "FixedRule"), new EvalGraderRow("judge", 2, "Model") }));
        Assert.That(eval.Dataset, Is.EqualTo(new EvalDatasetRow("meals", 2)));
        Assert.That(eval.Categories, Is.EqualTo(new[] { "town" }));
        Assert.That(eval.NeverRun, Is.True);
        Assert.That(eval.Runs, Is.Empty);
        Assert.That(list.Store, Is.EqualTo(new StoreRow(true, null)));
    }

    /// <summary>
    /// Once a run is recorded the eval lists it, with its outcome, its cost from the ledger and its modes' rates, and
    /// the modes and models it was run on.
    /// </summary>
    [Test]
    public async Task OnceARunIsRecorded_TheEvalListsIt()
    {
        var store = new InMemoryBenchmarkStore();
        var mode = new Mode(SampleMode.Beta, 1, [SampleToolset.Beta], SampleRole.Beta, null,
            new ModeRates(0.5, 0.25, 2, 0, 0));
        var model = new ModelUse(SampleMode.Beta, BenchmarkModelLevel.Agent, SampleRole.Beta, "model-a");
        await store.SaveRunAsync(new Run("r1", SampleEval.Beta, 2, BenchmarkRunStatus.Done, At, At.AddMinutes(5), 0,
            Modes: [mode], Total: new Total(1.25m, "USD", 900, 4), Models: [model]));

        var eval = (await ListAsync(store)).Evals.Single();

        Assert.That(eval.NeverRun, Is.False);
        var run = eval.Runs.Single();
        Assert.That((run.Run.Id, run.Run.Status, run.Run.Total?.Cost), Is.EqualTo(("r1", "done", (decimal?)1.25m)));
        Assert.That(run.Modes.Single().PassAtK, Is.EqualTo(0.5));
        Assert.That(eval.Modes, Is.EqualTo(new[] { SampleMode.Beta }));
        Assert.That(eval.Models, Is.EqualTo(new[] { "model-a" }));
    }

    /// <summary>Without a store nobody can say whether an eval ran, so the list doesn't claim it never did.</summary>
    [Test]
    public async Task WithoutAStore_NeverRunIsUnknown()
    {
        var list = await ListAsync(null);

        Assert.That(list.Evals.Single().NeverRun, Is.Null);
        Assert.That(list.Store, Is.EqualTo(new StoreRow(false, null)));
    }

    /// <summary>The list from handlers over a catalogue of one fake eval, as the runner would have listed it.</summary>
    private static async Task<EvalListReply> ListAsync(IBenchmarkStore? store)
    {
        var line = new EvalLine(SampleEval.Beta, 3, "A meal", SampleIdentity.Alpha, null,
            new EvalDatasetLine("meals", 2), ["town"],
            [
                new EvalGraderLine("priced", 1, BenchmarkGraderKind.FixedRule),
                new EvalGraderLine("judge", 2, BenchmarkGraderKind.Model)
            ]);
        var catalog = new BenchmarkCatalog(BenchmarkCatalog.Merge([line]));
        var handlers = new EvalHandlers(catalog, store, ApiTestHost.RepoRoot);

        var answer = await handlers.GetList(CancellationToken.None);

        return answer.Value!;
    }
}
