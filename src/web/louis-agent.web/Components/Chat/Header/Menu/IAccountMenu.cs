namespace LouisAgent.Web.Components.Chat.Header.Menu;

/// <summary>
/// What the chat's profile menu needs from accounts, and nothing more: whether this site has them, who's signed in,
/// the words for the two links, and the way out. The framework's web declares it; the account library implements
/// it (CL30), so the web ships without the account client and a site opts in.
/// </summary>
/// <remarks>
/// Separated Interface (Fowler): the interface lives with the code that needs it, the implementation with the code
/// that has it, so the dependency points the way the libraries ship: the account library on the web, never the
/// other way round. Without it the web would reference the account client to draw one menu.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0084-every-site-is-built-on-the-frameworks-web.md">ADR 0084: every site is built on the framework's web</seealso>
public interface IAccountMenu
{
    /// <summary>This site registered accounts and its API has them.</summary>
    bool Enabled { get; }

    bool SignedIn { get; }

    /// <summary>The person's display name, else their email; nothing for a guest.</summary>
    string? Name { get; }

    string? Email { get; }

    /// <summary>The site's words for the sign-in and register links.</summary>
    string SignInLabel { get; }

    string RegisterLabel { get; }

    /// <summary>Fires when who's signed in changes, so the menu draws again.</summary>
    event Action? Changed;

    /// <summary>Finds out who's signed in, once, when the menu first draws.</summary>
    Task LoadAsync();

    /// <summary>Signs the person out and takes them home.</summary>
    Task SignOutAsync();
}
