using LouisAgent.Choices;

namespace LouisAgent.Api.Evals.Runs.Kinds;

/// <summary>
/// Which scenarios a run asked for: the <c>dev</c> ones we tune against, the <c>held-out</c> ones we keep for before a
/// release, or <c>all</c>. It stays with the run, so a held-out run is never mistaken for a dev one.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum EvalRunSplit
{
    Dev,
    [WireName("held_out")] HeldOut,
    All,
}
