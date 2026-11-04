using LouisAgent.Composition.Messages;
using LouisAgent.Api.Contracts;
using LouisAgent.Api.Contracts.Admin.Checks;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Api.Contracts.Admin.Trends;
using LouisAgent.Api.Endpoints.Admin.Runs;
using LouisAgent.Api.Evals.List;
using LouisAgent.Api.Evals.Runs;
using LouisAgent.Api.Evals.Store;
using LouisAgent.Api.Evals.Store.Recording;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Messages;
using LouisAgent.Testing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LouisAgent.Api.Tests.Endpoints;

/// <summary>
/// Each admin handler declares what it can answer (ADR 0045), and a call answers one of those, with no HTTP in
/// between: the handler is a plain method on a plain object.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public class AdminHandlersTests
{
    private const string ManualPath = "src/evals/louis-agent.evals.runner/Manuals/stub.md";

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
        yield return new TestCaseData("GetRequests",
            typeof(Results<JsonHttpResult<LiveRequestsReply>, JsonHttpResult<ApiError>>));
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
            Inner(await Handlers(new InMemoryEvalStore()).GetTrend(SampleEval.Beta, CancellationToken.None));

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

    /// <summary>
    /// A run being recorded here says its requests so far (FX34), as the stored run's rows; one that isn't is a 404,
    /// and with no recorder at all it's a 503.
    /// </summary>
    [Test]
    public void TheRequestsSoFar_AreARecordedRunsRows_OrA404()
    {
        // A run recorded with no ledger: it is there, and has asked nothing yet.
        var recorder = new EvalRecorder(new InMemoryEvalStore());
        recorder.Attach(new RunningEval("run123456789", "stub", 1));
        var handlers = Handlers(recorder: recorder);

        var answer = Inner(handlers.GetRequests("run123456789"));

        Assert.That(((JsonHttpResult<LiveRequestsReply>)answer).Value!.Requests, Is.Empty);
        Assert.That(Status(handlers.GetRequests("other")), Is.EqualTo(StatusCodes.Status404NotFound));
        Assert.That(Status(Handlers().GetRequests("run123456789")),
            Is.EqualTo(StatusCodes.Status503ServiceUnavailable));
    }

    /// <summary>
    /// A ledger that can't be read just now is a 503 that says why, so the page keeps the costs it has.
    /// </summary>
    [Test]
    public void ALedgerThatCantBeRead_IsA503()
    {
        string ledger = Path.Combine(Path.GetTempPath(), "louis-usage-ledger-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(ledger);
        try
        {
            // This month's ledger file with a line cut off half way.
            File.WriteAllText(Path.Combine(ledger, $"usage-{DateTime.UtcNow:yyyy-MM}.jsonl"), "{\"at\":");
            var recorder = new EvalRecorder(new InMemoryEvalStore(), ledgerDirectory: ledger);
            recorder.Attach(new RunningEval("run123456789", "stub", 1));

            var answer = Inner(Handlers(recorder: recorder).GetRequests("run123456789"));

            var error = (JsonHttpResult<ApiError>)answer;
            Assert.That(error.StatusCode, Is.EqualTo(StatusCodes.Status503ServiceUnavailable));
            Assert.That(error.Value!.Error.Message,
                Is.Not.EqualTo(MessageText.Format(ApiUserText.NoBenchmarkStore)), "it says why, not \"no store\"");
        }
        finally
        {
            Directory.Delete(ledger, recursive: true);
        }
    }

    /// <summary>
    /// The handlers over a catalogue of one check with a manual, and the store and the recorder given, if any.
    /// </summary>
    private static AdminHandlers Handlers(IEvalStore? store = null, EvalRecorder? recorder = null)
    {
        var catalog = new EvalList([new EvalEntry("stub", "A stub", ManualPath)]);
        var runner = new EvalProcess(catalog, ApiTestHost.RepoRoot, "http://127.0.0.1:1/");

        return new AdminHandlers(catalog, runner, "admin-secret", store, recorder,
            _ => new Dictionary<string, string>(), ApiTestHost.RepoRoot);
    }

    /// <summary>The answer a declared set of answers holds, or the answer itself.</summary>
    private static IResult Inner(object answer) => answer is INestedHttpResult nested ? nested.Result : (IResult)answer;

    /// <summary>The status an error answer carries.</summary>
    private static int? Status(object answer) =>
        Inner(answer) is JsonHttpResult<ApiError> error ? error.StatusCode : null;
}
