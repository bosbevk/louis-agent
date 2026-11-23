using System.Text.Json.Serialization;

namespace LouisAgent.Evals.Contracts.Runs;

/// <summary>
/// Where a run is: still going, finished, or stopped before the end because the harness failed, not the agent.
/// Its wire names are .NET's own attribute, as <see cref="RunKind"/>'s are.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum RunStatus
{
    /// <summary>The run is still playing its conversations.</summary>
    [JsonStringEnumMemberName("running")] Running,

    /// <summary>Every conversation was played and graded.</summary>
    [JsonStringEnumMemberName("finished")] Finished,

    /// <summary>The run stopped early, and says why in its <c>StoppedBecause</c>.</summary>
    [JsonStringEnumMemberName("could-not-finish")]
    CouldNotFinish,

    /// <summary>A person stopped it while it went; what was graded before the stop is kept (F126.3.4).</summary>
    [JsonStringEnumMemberName("stopped")] Stopped
}
