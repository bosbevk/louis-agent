using LouisAgent.Api.Services;

namespace LouisAgent.Api.Config;

/// <summary>
/// The admin portal's settings (F27, F31), read once at start-up on <c>AgentOptions.FromEnvironment</c>'s pattern: a
/// blank value is no value, every value trimmed.
/// </summary>
/// <param name="AdminKey">The portal's key: <c>ADMIN_API_KEY</c>, else <c>AGENT_API_KEY</c>; neither keeps it closed.</param>
/// <param name="BenchmarkApiUrl">Where a check's script calls this API back (<c>BENCHMARK_API_URL</c>; the container's own port).</param>
/// <param name="BenchmarksMongoUrl">The benchmark store (<c>BENCHMARKS_MONGO_URL</c>, in <c>.env.secrets</c>); none: runs aren't started.</param>
/// <param name="BenchmarksDatabase">Its database (<c>BENCHMARKS_MONGO_DATABASE</c>; <c>puls_benchmarks</c>, the tests' <c>puls_benchmarks_test</c>).</param>
public sealed record AdminOptions(string? AdminKey, string BenchmarkApiUrl, string? BenchmarksMongoUrl, string BenchmarksDatabase)
{
    public const string DefaultBenchmarkApiUrl = "http://127.0.0.1:8080/";
    public const string DefaultBenchmarksDatabase = "puls_benchmarks";

    public static AdminOptions FromEnvironment(Func<string, string?>? getVariable = null)
    {
        getVariable ??= Environment.GetEnvironmentVariable;
        string? Get(string name) => string.IsNullOrWhiteSpace(getVariable(name)) ? null : getVariable(name)!.Trim();

        return new AdminOptions(
            AdminGate.Key(Get("ADMIN_API_KEY"), Get("AGENT_API_KEY")),
            Get("BENCHMARK_API_URL") ?? DefaultBenchmarkApiUrl,
            Get("BENCHMARKS_MONGO_URL"),
            Get("BENCHMARKS_MONGO_DATABASE") ?? DefaultBenchmarksDatabase);
    }
}
