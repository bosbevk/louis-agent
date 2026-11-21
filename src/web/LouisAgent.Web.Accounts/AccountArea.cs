using LouisAgent.Web.Accounts.Client;
using LouisAgent.Web.Components.Areas;

namespace LouisAgent.Web.Accounts;

/// <summary>
/// The accounts as an area a site names (F127.6): <c>.Accounts(layout)</c> registers sign in, register and the
/// profile, their pages, and the layout the account pages draw inside.
/// </summary>
/// <remarks>
/// An extension on the framework's <see cref="SiteAreasBuilder"/>, from this library, since the framework's web can't
/// name the accounts (dependencies point one way); it calls <see cref="AccountServices.AddLouisAgentAccounts"/>.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0084-every-site-is-built-on-the-frameworks-web.md">ADR 0084: every site is built on the framework's web</seealso>
public static class AccountArea
{
    /// <summary>The accounts, their pages drawn inside <paramref name="layout"/>.</summary>
    public static SiteAreasBuilder Accounts(this SiteAreasBuilder areas, Type layout) =>
        areas.Add(new SiteArea("accounts", services => services.AddLouisAgentAccounts(), [AccountLibrary.Assembly],
            AccountLayout: layout));
}
