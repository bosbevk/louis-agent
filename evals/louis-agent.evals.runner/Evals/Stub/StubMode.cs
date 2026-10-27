using LouisAgent.Choices;

namespace LouisAgent.Evals.Runner.Evals.Stub;

/// <summary>The modes the stub eval runs in (ADR 0043: an eval set keeps its own enum of the names it uses). A member's wire name is the mode as a run's protocol and the store carry it.</summary>
public enum StubMode
{
    [WireName("puls-only")] Only,
    [WireName("puls-cook")] Cook,
}
