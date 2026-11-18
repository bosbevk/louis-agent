using LouisAgent.Web.Components.Site.Tables;

namespace LouisAgent.Web.Framework.Content.Comparison;

/// <summary>
/// One row of the comparison (F161.1, step 12, ADR 0110): what is compared, louis-agent's cell and the file in the repo
/// that proves it, and what Microsoft Agent Framework's and Semantic Kernel's own pages say, or "Not stated".
/// </summary>
public sealed record ComparisonClaim(
    string Topic,
    string Ours,
    string Proof,
    string AgentFramework,
    string SemanticKernel)
{
    /// <summary>The claim as the comparison table draws it, a cell under each of the three.</summary>
    public ComparisonRow Row => new(Topic, [Ours, AgentFramework, SemanticKernel]);
}
