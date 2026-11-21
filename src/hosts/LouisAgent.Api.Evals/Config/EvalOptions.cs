namespace LouisAgent.Api.Evals.Config;

/// <summary>
/// The studio's settings (CL57, out of the API's admin options): where a run's script calls the API back, the store
/// the runs and grades are kept in, and the runner the API starts an eval from.
/// </summary>
/// <param name="BenchmarkApiUrl">BENCHMARK_API_URL: the address a run's script calls this API back at.</param>
/// <param name="EvalsMongoUrl">EVALS_MONGO_URL: the store; unset, there is none and no run starts.</param>
/// <param name="EvalsDatabase">EVALS_MONGO_DATABASE: the store's database.</param>
/// <param name="EvalRunner">EVAL_RUNNER: the runner's dll the API lists and runs evals with.</param>
public sealed record EvalOptions(
    string BenchmarkApiUrl,
    string? EvalsMongoUrl,
    string EvalsDatabase,
    string EvalRunner)
{
    /// <summary>Where the runner is in the container.</summary>
    public const string DefaultEvalRunner = "/app/evals/eval-runner.dll";

    /// <summary>The container's own port, where a run's script finds this API.</summary>
    public const string DefaultBenchmarkApiUrl = "http://127.0.0.1:8080/";

    /// <summary>The store's database when none is named.</summary>
    public const string DefaultEvalsDatabase = "evals";

    /// <summary>Reads the four settings, each with its default when unset.</summary>
    public static EvalOptions FromEnvironment(Func<string, string?>? getVariable = null)
    {
        getVariable ??= Environment.GetEnvironmentVariable;

        // A blank setting is the same as a missing one; anything else comes back trimmed.
        string? Get(string name) => string.IsNullOrWhiteSpace(getVariable(name)) ? null : getVariable(name)!.Trim();

        return new EvalOptions(
            Get("BENCHMARK_API_URL") ?? DefaultBenchmarkApiUrl,
            // The store's names before the rename are read too, after the new ones, until a TODO line removes them.
            Get("EVALS_MONGO_URL") ?? Get("BENCHMARKS_MONGO_URL"),
            Get("EVALS_MONGO_DATABASE") ?? Get("BENCHMARKS_MONGO_DATABASE") ?? DefaultEvalsDatabase,
            Get("EVAL_RUNNER") is { } runner ? Path.GetFullPath(runner) : DefaultEvalRunner);
    }
}
