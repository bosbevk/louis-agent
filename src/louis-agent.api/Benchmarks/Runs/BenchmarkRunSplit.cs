using System.Text.Json.Serialization;

namespace LouisAgent.Api.Benchmarks.Runs;

/// <summary>
/// Which scenarios a run asked for (F18 step 5.4 review; ADR 0024: parsed once where it enters): the <c>dev</c> ones it tunes against, the
/// <c>held-out</c> ones before a release, or <c>all</c>. Kept on the run, so a held-out run is never read as a dev one.
/// </summary>
public enum BenchmarkRunSplit
{
    Dev,
    [JsonStringEnumMemberName("held_out")] HeldOut,
    All,
}
