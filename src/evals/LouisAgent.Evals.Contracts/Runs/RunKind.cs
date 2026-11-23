using System.Text.Json.Serialization;

namespace LouisAgent.Evals.Contracts.Runs;

/// <summary>
/// How a run asked the agent: live, which calls the models and is recorded, or a replay of a recording, which is
/// free. Its wire names are .NET's own attribute, which <c>[WireName]</c> is underneath, so the contracts reference
/// nothing and <c>.Wire()</c> and <c>SettingChoice</c> still read them.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum RunKind
{
    /// <summary>The agent and the judge were asked for real.</summary>
    [JsonStringEnumMemberName("live")] Live,

    /// <summary>The answers came from a live run's recording.</summary>
    [JsonStringEnumMemberName("replay")] Replay
}
