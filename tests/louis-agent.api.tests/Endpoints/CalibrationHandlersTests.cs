using LouisAgent.Api.Contracts;
using LouisAgent.Api.Contracts.Admin.Reviews;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Api.Endpoints.Admin.Reviews;
using LouisAgent.Api.Evals.Calibrations;
using LouisAgent.Api.Evals.Calibrations.Store;
using LouisAgent.Api.Evals.List;
using LouisAgent.Api.Evals.Reviews.Store;
using LouisAgent.Api.Evals.Runs;
using LouisAgent.Api.Evals.Store;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LouisAgent.Api.Tests.Endpoints;

/// <summary>
/// The judge-check handlers declare what they answer (ADR 0045), and answer it with no HTTP in between. Starting a
/// check for real runs the eval runner, which the integration tests do.
/// </summary>
public class CalibrationHandlersTests
{
    /// <summary>What each handler says it answers.</summary>
    private static IEnumerable<TestCaseData> Declared()
    {
        yield return new TestCaseData("StartCalibration",
            typeof(Task<Results<JsonHttpResult<LiveRunRow>, JsonHttpResult<ApiError>>>));
        yield return new TestCaseData("GetCalibrations",
            typeof(Task<Results<JsonHttpResult<CalibrationsReply>, JsonHttpResult<ApiError>>>));
    }

    /// <summary>A handler's signature names every answer it can give.</summary>
    [TestCaseSource(nameof(Declared))]
    public void AHandler_DeclaresItsAnswers(string handler, Type answers)
    {
        Assert.That(typeof(CalibrationHandlers).GetMethod(handler)?.ReturnType, Is.EqualTo(answers));
    }

    /// <summary>Without the stores, neither handler can do anything: a 503.</summary>
    [Test]
    public async Task WithoutTheStores_BothAre503()
    {
        var handlers = Handlers(withStores: false);

        Assert.That(Status(await handlers.StartCalibration("run_1", "meal", CancellationToken.None)),
            Is.EqualTo(StatusCodes.Status503ServiceUnavailable));
        Assert.That(Status(await handlers.GetCalibrations("run_1", CancellationToken.None)),
            Is.EqualTo(StatusCodes.Status503ServiceUnavailable));
    }

    /// <summary>A run the store doesn't hold is a 404 for both.</summary>
    [Test]
    public async Task AnUnknownRun_Is404()
    {
        var handlers = Handlers(withStores: true);

        Assert.That(Status(await handlers.StartCalibration("ghost", "meal", CancellationToken.None)),
            Is.EqualTo(StatusCodes.Status404NotFound));
        Assert.That(Status(await handlers.GetCalibrations("ghost", CancellationToken.None)),
            Is.EqualTo(StatusCodes.Status404NotFound));
    }

    /// <summary>The handlers over empty stores, or none.</summary>
    private static CalibrationHandlers Handlers(bool withStores)
    {
        var catalog = new EvalList();
        var runner = new EvalProcess(catalog, ApiTestHost.RepoRoot, "http://127.0.0.1:1/");
        var calibrations = new InMemoryCalibrationStore();

        return withStores
            ? new CalibrationHandlers(catalog, runner, new InMemoryEvalStore(), new InMemoryHumanReviewStore(),
                calibrations, new CalibrationRecorder(calibrations))
            : new CalibrationHandlers(catalog, runner, null, null, null, null);
    }

    /// <summary>The status an error answer carries.</summary>
    /// <remarks>
    /// Two plain steps instead of a ternary inside a ternary (Fowler's <i>Decompose Conditional</i>): first unwrap the
    /// answer, then read its status, so each line tests one thing.
    /// </remarks>
    private static int? Status(object answer)
    {
        // A declared set of answers holds the real answer inside it.
        var inner = answer is INestedHttpResult nested ? nested.Result : answer;

        return inner is JsonHttpResult<ApiError> error ? error.StatusCode : null;
    }
}
