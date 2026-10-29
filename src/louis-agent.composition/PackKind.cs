using LouisAgent.Choices;

namespace LouisAgent.Composition;

/// <summary>
/// The compiled packs a host may load with <c>PACKS</c>, or an identity may name (ADR 0024). Step 4 adds <c>Shops</c>;
/// F35 <c>Recipes</c>, CL16 <c>Cooking</c>, F40 <c>Customers</c>. An MCP server is data, never a member: F36 adds
/// <c>MCP_PACKS</c> as a second source beside this one.
/// </summary>
public enum PackKind
{
    [WireName("shops")] Shops,
}
