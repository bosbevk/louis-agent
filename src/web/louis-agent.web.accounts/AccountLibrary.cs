using System.Reflection;

namespace LouisAgent.Web.Accounts;

/// <summary>
/// Where the account pages are (CL57): the assembly a site's router lists, through <c>FrameworkPages.With</c>, when
/// the site opted into accounts. Without it the sign-in, registration and profile pages answer nothing, which is
/// what every site did until now.
/// </summary>
public static class AccountLibrary
{
    /// <summary>The library's assembly, for the router.</summary>
    public static readonly Assembly Assembly = typeof(AccountLibrary).Assembly;
}
