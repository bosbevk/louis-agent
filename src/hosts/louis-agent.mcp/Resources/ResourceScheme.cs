namespace LouisAgent.Mcp.Resources;

/// <summary>
/// The scheme at the start of a resource's URI, the part before <c>://</c> (ADR 0043: a repeated name is an enum).
/// </summary>
public enum ResourceScheme
{
    /// <summary><c>feed://shop-prices</c>: data a pack publishes, read whole as JSON (ADR 0070, F56).</summary>
    Feed,
}
