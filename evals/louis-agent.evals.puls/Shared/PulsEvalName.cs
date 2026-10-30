using LouisAgent.Choices;

namespace LouisAgent.Evals.Puls.Shared;

/// <summary>
/// The evals the Puls set offers in C#, by the names they are listed, run and stored under (ADR 0043: each eval set
/// keeps its own enum of the names it uses). The routing eval is a folder of files beside the identity (F49.2), named
/// in its file.
/// </summary>
public enum PulsEvalName
{
    [WireName("puls-cook")] Cook
}
