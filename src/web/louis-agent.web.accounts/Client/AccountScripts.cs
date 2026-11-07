namespace LouisAgent.Web.Accounts.Client;

/// <summary>
/// Where the account library's modules are, named once (CL31), as the framework's <c>FrameworkScripts</c> names its
/// own: a component builds its <c>JsModule</c> from one of these, and a site lists no script for it.
/// </summary>
public static class AccountScripts
{
    /// <summary>The library's content path, as a site's page sees it.</summary>
    private const string Content = "./_content/louis-agent.web.accounts/";

    /// <summary>The browser's WebAuthn calls: whether it can do passkeys, make one, sign with one.</summary>
    public const string Passkeys = Content + "js/passkeys.js";
}
