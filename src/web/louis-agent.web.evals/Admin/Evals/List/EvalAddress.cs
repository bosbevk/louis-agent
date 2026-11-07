namespace LouisAgent.Web.Evals.Admin.Evals.List;

/// <summary>An eval's address on the Evals page: the page with the eval in its query, written in one place.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0065-the-view-is-in-the-address.md">ADR 0065: the view is in the address</seealso>
public static class EvalAddress
{
    /// <summary>The Evals page open on this eval.</summary>
    public static string Of(string eval) => $"admin?eval={Uri.EscapeDataString(eval)}";
}
