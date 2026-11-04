using LouisAgent.Choices;

namespace LouisAgent.Evals.Runner.Evals.Stub;

/// <summary>The roles the stub eval names (ADR 0043: an eval set keeps its own enum of the names it uses): the voice its modes open with, and the specialist its delegating mode hands the question to.</summary>
public enum StubRole
{
    [WireName("stub/voice")] Voice,
    [WireName("stub-specialist")] Specialist,
}
