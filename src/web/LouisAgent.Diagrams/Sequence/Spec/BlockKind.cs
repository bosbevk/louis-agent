using LouisAgent.Choices;

namespace LouisAgent.Diagrams.Sequence.Spec;

/// <summary>What a block's frame says about its steps: they repeat, they may run, or one case of several runs.</summary>
public enum BlockKind
{
    [WireName("loop")] Loop,
    [WireName("opt")] Opt,
    [WireName("alt")] Alt
}
