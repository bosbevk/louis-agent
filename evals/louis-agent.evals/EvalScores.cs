using System.Text.Json;

namespace LouisAgent.Evals;

/// <summary>
/// An eval's scores for one conversation: written as snake_case JSON into the protocol's <c>conversation</c> line. Each eval
/// has its own record (the cook's, the routing's).
/// </summary>
public abstract record EvalScores
{
    /// <summary>The options every score is written with: snake_case names (routed_right, priced_ratio), shared by the runner and the tests.</summary>
    public static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
}
