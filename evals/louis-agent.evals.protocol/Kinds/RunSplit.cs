using LouisAgent.Choices;

namespace LouisAgent.Evals.Protocol.Kinds;

/// <summary>
/// Which scenarios a run asked for: the ones the prompts are tuned on, the ones kept back to check them, or all.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0043-repeated-names-are-enums.md">ADR 0043: repeated names are enums</seealso>
public enum RunSplit
{
    [WireName("dev")] Dev,
    [WireName("held-out")] HeldOut,
    [WireName("all")] All,
}
