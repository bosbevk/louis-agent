using System.Text.Json;

namespace LouisAgent.Api.Benchmarks.Runs;

/// <summary>A <see cref="BenchmarkRunSplit"/> as the protocol's first line and the store name it: the enum's name in snake case (<c>held_out</c>).</summary>
public static class BenchmarkRunSplitName
{
    public static string Of(BenchmarkRunSplit split) => JsonNamingPolicy.SnakeCaseLower.ConvertName(split.ToString());

    /// <summary>The snake-case names as aliases, for <c>SettingChoice</c> (the enum's own names match by themselves).</summary>
    public static readonly IReadOnlyDictionary<string, BenchmarkRunSplit> Aliases =
        Enum.GetValues<BenchmarkRunSplit>().ToDictionary(Of);
}
