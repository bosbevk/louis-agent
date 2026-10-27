using LouisAgent.Choices;

namespace LouisAgent.Evals.Puls.Shared;

/// <summary>The evals the Puls set offers, by the names they are listed, run and stored under (ADR 0043: each eval set keeps its own enum of the names it uses).</summary>
public enum PulsEvalName
{
    [WireName("puls-cook")] Cook,
    [WireName("puls-routing")] Routing,
}
