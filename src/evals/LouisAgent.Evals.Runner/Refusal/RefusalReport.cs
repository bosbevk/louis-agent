namespace LouisAgent.Evals.Runner.Refusal;

/// <summary>
/// What the runner does with a refusal (F18 step 4.4 review): the reason goes to stderr, and the exit code is 1. The refusals are the
/// runner's own (<see cref="RunnerException"/>) and the harness's while the catalogue is built (a dataset that won't load).
/// </summary>
public static class RefusalReport
{
    /// <summary>Whether an error is a refusal: the runner's own, or the harness's while the catalogue is built (a dataset that won't load).</summary>
    public static bool IsRefusal(Exception error) => error is RunnerException or InvalidDataException;

    /// <summary>Writes the refusal's reason to stderr and returns the exit code.</summary>
    public static int Report(Exception error, TextWriter stderr)
    {
        stderr.WriteLine(error.Message);
        return 1;
    }
}
