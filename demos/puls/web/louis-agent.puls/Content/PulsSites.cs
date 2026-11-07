using LouisAgent.Web.Admin.Explorer.Sites;

namespace LouisAgent.Puls.Content;

/// <summary>
/// The websites that matter to Puls, for the eval pages' sources chart: the shops the cook prices at, where a price is
/// one you can pay, other South African sites, and the map it looks a branch up on, which says where and not how much.
/// </summary>
public static class PulsSites
{
    /// <summary>The kinds, shops first.</summary>
    public static readonly SiteKinds Kinds = new(
    [
        new SiteKind("SA shops", "checkers.co.za", "pnp.co.za", "woolworths.co.za", "spar.co.za", "shoprite.co.za",
            "usave.co.za"),
        new SiteKind("other SA", ".za"),
        new SiteKind("maps", "openstreetmap.org")
    ]);
}
