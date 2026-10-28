using LouisAgent.Api.Admin;

namespace LouisAgent.Api.Config;

/// <summary>
/// The admin portal's settings, read once at start-up the way <c>AgentOptions.FromEnvironment</c> reads the agent's:
/// a blank value counts as no value, and every value is trimmed.
/// </summary>
/// <param name="AdminKey">
/// The portal's key: <c>ADMIN_API_KEY</c>, else <c>AGENT_API_KEY</c>; with neither the portal stays closed.
/// </param>
/// <param name="BenchmarkApiUrl">
/// Where the eval runner calls this API back (<c>BENCHMARK_API_URL</c>; the container's own port).
/// </param>
/// <param name="EvalsMongoUrl">
/// The store runs are recorded in (<c>EVALS_MONGO_URL</c>, in <c>.env.secrets</c>); without it no run starts.
/// </param>
/// <param name="EvalsDatabase">
/// Its database (<c>EVALS_MONGO_DATABASE</c>, else <c>evals</c>). An instance can keep its old database by naming it.
/// </param>
/// <param name="EvalRunner">
/// The eval runner's dll (<c>EVAL_RUNNER</c>). A relative path is made absolute here, once, so a run started from
/// another folder still finds it.
/// </param>
public sealed record AdminOptions(
    string? AdminKey,
    string BenchmarkApiUrl,
    string? EvalsMongoUrl,
    string EvalsDatabase,
    string EvalRunner)
{
    /// <summary>Where the eval runner's dll sits in the image; the API starts it with <c>dotnet</c>.</summary>
    public const string DefaultEvalRunner = "/app/evals/louis-agent.evals.runner.dll";

    /// <summary>The API's own port inside the container, where a run calls back when nothing else is set.</summary>
    public const string DefaultBenchmarkApiUrl = "http://127.0.0.1:8080/";

    /// <summary>
    /// The database runs go in when none is named. An instance that already has runs names its own database, so they
    /// stay where they are.
    /// </summary>
    public const string DefaultEvalsDatabase = "evals";

    /// <summary>
    /// Reads the portal's settings from the environment, or from the lookup a test hands in, filling in the defaults.
    /// </summary>
    public static AdminOptions FromEnvironment(Func<string, string?>? getVariable = null)
    {
        getVariable ??= Environment.GetEnvironmentVariable;

        // A blank setting is the same as a missing one; anything else comes back trimmed.
        string? Get(string name) => string.IsNullOrWhiteSpace(getVariable(name)) ? null : getVariable(name)!.Trim();

        return new AdminOptions(
            AdminGate.Key(Get("ADMIN_API_KEY"), Get("AGENT_API_KEY")),
            Get("BENCHMARK_API_URL") ?? DefaultBenchmarkApiUrl,
            // The store's names before the rename are read too, after the new ones, until a TODO line removes them.
            Get("EVALS_MONGO_URL") ?? Get("BENCHMARKS_MONGO_URL"),
            Get("EVALS_MONGO_DATABASE") ?? Get("BENCHMARKS_MONGO_DATABASE") ?? DefaultEvalsDatabase,
            Get("EVAL_RUNNER") is { } runner ? Path.GetFullPath(runner) : DefaultEvalRunner);
    }
}
