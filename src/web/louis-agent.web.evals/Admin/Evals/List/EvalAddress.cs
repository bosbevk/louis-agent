namespace LouisAgent.Web.Evals.Admin.Evals.List;

/// <summary>
/// The Evals page's address, and an eval's on it (the page with the eval in its query), written in one place. Since
/// F144 the page is under the admin, at <c>admin/evals</c>, since the admin's root is its home.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0065-the-view-is-in-the-address.md">ADR 0065: the view is in the address</seealso>
public static class EvalAddress
{
    /// <summary>The Evals page: the sidebar's link, the run page's way back and the card's Open.</summary>
    public const string Studio = "admin/evals";

    /// <summary>The Evals page open on this eval.</summary>
    public static string Of(string eval) => $"{Studio}?eval={Uri.EscapeDataString(eval)}";
}
