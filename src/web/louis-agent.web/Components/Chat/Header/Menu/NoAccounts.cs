namespace LouisAgent.Web.Components.Chat.Header.Menu;

/// <summary>The menu's accounts on a site that has none: nobody is signed in, and the guest's menu is drawn as before.</summary>
/// <remarks>
/// Null Object: the menu holds one of these instead of a null and asks it the same questions, so it has no "if there
/// are no accounts" branch of its own and a test of the chat needs no account services at all.
/// </remarks>
public sealed class NoAccounts : IAccountMenu
{
    public static readonly NoAccounts Instance = new();

    private NoAccounts()
    {
    }

    public bool Enabled => false;

    public bool SignedIn => false;

    public string? Name => null;

    public string? Email => null;

    public string SignInLabel => "";

    public string RegisterLabel => "";

    /// <summary>Nothing ever changes here, so nobody is kept to tell.</summary>
    public event Action? Changed
    {
        add { }
        remove { }
    }

    public Task LoadAsync() => Task.CompletedTask;

    public Task SignOutAsync() => Task.CompletedTask;
}
