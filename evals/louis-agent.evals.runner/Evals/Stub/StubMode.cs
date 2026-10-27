using LouisAgent.Choices;

namespace LouisAgent.Evals.Runner.Evals.Stub;

/// <summary>The modes the stub eval runs in: it answers by itself (<c>Direct</c>) or hands the question to its specialist (<c>Delegating</c>) (ADR 0043: an eval set keeps its own enum of the names it uses). A member's wire name is the mode as a run's protocol and the store carry it.</summary>
public enum StubMode
{
    [WireName("stub-direct")] Direct,
    [WireName("stub-delegating")] Delegating,
}
