namespace LouisAgent.Evals.Running;

/// <summary>
/// Which of a grader's errors stop the run (F18 step 6.1): only the run's own cancel. A judge's timeout surfaces as an <see cref="OperationCanceledException"/> too
/// (HttpClient's), while the run's token is still live; that is the grader's trouble and an Unknown grade, as <see cref="TurnFailures"/> treats a turn's.
/// </summary>
public static class GraderFailures
{
    public static bool IsRunCancel(Exception error, CancellationToken run) => error is OperationCanceledException && run.IsCancellationRequested;
}
