using System.Text.Json;

namespace LouisAgent.Api.Benchmarks.Runs;

/// <summary>A <see cref="BenchmarkTurnFailure"/> as the protocol's <c>turn</c> line and the store name it: the enum's name in snake case (<c>api_error</c>).</summary>
public static class BenchmarkTurnFailureName
{
    public static string Of(BenchmarkTurnFailure failure) => JsonNamingPolicy.SnakeCaseLower.ConvertName(failure.ToString());

    /// <summary>The snake-case names as aliases, for <c>SettingChoice</c> (the enum's own names match by themselves).</summary>
    public static readonly IReadOnlyDictionary<string, BenchmarkTurnFailure> Aliases =
        Enum.GetValues<BenchmarkTurnFailure>().ToDictionary(Of);
}
