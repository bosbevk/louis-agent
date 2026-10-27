using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using LouisAgent.Api.Contracts;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Endpoints;
using LouisAgent.Testing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LouisAgent.Api.Tests.Endpoints;

/// <summary>
/// Each admin handler declares what it can answer (ADR 0045), and a call answers one of those, with no HTTP in
/// between: the handler is a plain method on a plain object.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public class AdminHandlersTests
{
    private const string ManualPath = "evals/louis-agent.evals.runner/Manuals/stub.md";

    /// <summary>What each handler says it answers, as its signature declares it.</summary>
    private static IEnumerable<TestCaseData> Declared()
    {
        yield return new TestCaseData("GetList", typeof(Task<JsonHttpResult<BenchmarkListReply>>));
        yield return new TestCaseData("GetManual",
            typeof(Results<JsonHttpResult<ManualReply>, JsonHttpResult<ApiError>>));
        yield return new TestCaseData("GetTrend",
            typeof(Task<Results<JsonHttpResult<TrendReply>, JsonHttpResult<ApiError>>>));
        yield return new TestCaseData("StartRun",
            typeof(Results<JsonHttpResult<LiveRunRow>, JsonHttpResult<ApiError>>));
        yield return new TestCaseData("GetCurrent",
            typeof(Results<JsonHttpResult<LiveRunRow>, JsonHttpResult<ApiError>>));
        yield return new TestCaseData("GetEvents",
            typeof(Results<ServerSentEventsResult<string>, JsonHttpResult<ApiError>>));
    }

    /// <summary>A handler's signature names every answer it can give.</summary>
    [TestCaseSource(nameof(Declared))]
    public void AHandler_DeclaresItsAnswers(string handler, Type answers)
    {
        Assert.That(typeof(AdminHandlers).GetMethod(handler)?.ReturnType, Is.EqualTo(answers));
    }

    /// <summary>Without a store the list still names the checks, and says the store isn't configured.</summary>
    [Test]
    public async Task TheList_WithoutAStore_NamesTheChecks()
    {
        var answer = Inner(await Handlers().GetList(CancellationToken.None));

        var list = ((JsonHttpResult<BenchmarkListReply>)answer).Value!;
        Assert.That(list.Checks.Select(c => c.Id), Is.EqualTo(new[] { "stub" }));
        Assert.That(list.Store, Is.EqualTo(new StoreRow(false, null)));
    }

    /// <summary>A known check's manual is the manual; a check we don't know is a 404.</summary>
    [Test]
    public void TheManual_IsTheManual_OrA404()
    {
        var handlers = Handlers();

        Assert.That(Inner(handlers.GetManual("stub")), Is.InstanceOf<JsonHttpResult<ManualReply>>());
        Assert.That(Status(handlers.GetManual("ghost")), Is.EqualTo(StatusCodes.Status404NotFound));
    }

    /// <summary>The trend needs the store: a 503 without one, the trend with one.</summary>
    [Test]
    public async Task TheTrend_NeedsTheStore()
    {
        Assert.That(Status(await Handlers().GetTrend(SampleEval.Beta, CancellationToken.None)),
            Is.EqualTo(StatusCodes.Status503ServiceUnavailable));

        var answer =
            Inner(await Handlers(new InMemoryBenchmarkStore()).GetTrend(SampleEval.Beta, CancellationToken.None));

        Assert.That(answer, Is.InstanceOf<JsonHttpResult<TrendReply>>());
    }

    /// <summary>A run of a check we don't know is a 404, and one with no store to record it is a 503.</summary>
    [Test]
    public void StartingARun_IsRefused_WithoutACheck_OrAStore()
    {
        var handlers = Handlers();

        Assert.That(Status(handlers.StartRun("ghost", null)), Is.EqualTo(StatusCodes.Status404NotFound));
        Assert.That(Status(handlers.StartRun("stub", null)), Is.EqualTo(StatusCodes.Status503ServiceUnavailable));
    }

    /// <summary>With no run started, there is no current run and no events to follow.</summary>
    [Test]
    public void WithNoRun_TheCurrentRunAndItsEvents_Are404()
    {
        var handlers = Handlers();

        Assert.That(Status(handlers.GetCurrent()), Is.EqualTo(StatusCodes.Status404NotFound));
        Assert.That(Status(handlers.GetEvents("live_1", CancellationToken.None)),
            Is.EqualTo(StatusCodes.Status404NotFound));
    }

    /// <summary>The handlers over a catalogue of one check with a manual, and the store given, if any.</summary>
    private static AdminHandlers Handlers(IBenchmarkStore? store = null)
    {
        var catalog = new BenchmarkCatalog([new BenchmarkCheck("stub", "A stub", ManualPath)]);
        var runner = new BenchmarkRunner(catalog, ApiTestHost.RepoRoot, "http://127.0.0.1:1/");
        return new AdminHandlers(catalog, runner, "admin-secret", store, null,
            _ => new Dictionary<string, string>(), ApiTestHost.RepoRoot);
    }

    /// <summary>The answer a declared set of answers holds, or the answer itself.</summary>
    private static IResult Inner(object answer) => answer is INestedHttpResult nested ? nested.Result : (IResult)answer;

    /// <summary>The status an error answer carries.</summary>
    private static int? Status(object answer) =>
        Inner(answer) is JsonHttpResult<ApiError> error ? error.StatusCode : null;
}
