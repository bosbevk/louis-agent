using LouisAgent.Evals.Api;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Running;

/// <summary>
/// Which exceptions a turn ends with are the infrastructure's, not the agent's or the harness's (F18 step 5.1): the failure each one
/// is recorded as, or null for anything else, which stays thrown. A cancelled run is never a failure; its token says so.
/// </summary>
public static class TurnFailures
{
    public static TrialFailure? Of(Exception error, CancellationToken run) => error switch
    {
        OperationCanceledException when run.IsCancellationRequested => null,
        // HttpClient's own timeout surfaces as a TaskCanceledException while the run's token is still live.
        OperationCanceledException or TimeoutException => TrialFailure.Timeout,
        EvalApiException => TrialFailure.ApiError,
        HttpRequestException or IOException => TrialFailure.Transport,
        _ => null,
    };
}
