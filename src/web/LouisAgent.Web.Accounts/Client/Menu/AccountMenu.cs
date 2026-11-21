using LouisAgent.Web.Accounts.Client.Navigation;
using LouisAgent.Web.Accounts.Text;
using LouisAgent.Web.Components.Chat.Header.Menu;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Accounts.Client.Menu;

/// <summary>
/// The chat menu's view of accounts on a site that has them: who the state says is signed in, the site's words for
/// the links, and sign out through the API. Registered by <see cref="AccountServices.AddLouisAgentAccounts"/> in
/// place of the menu's "no accounts".
/// </summary>
public sealed class AccountMenu(AccountState state, AccountApi api, AccountText text, NavigationManager navigation)
    : IAccountMenu
{
    public bool Enabled => state.Enabled;

    public bool SignedIn => state.SignedIn;

    public string? Name => state.Me?.DisplayName ?? state.Me?.Email;

    public string? Email => state.Me?.Email;

    public string SignInLabel => text.SignIn;

    public string RegisterLabel => text.Register;

    /// <summary>The state's own event, passed through.</summary>
    public event Action? Changed
    {
        add => state.Changed += value;
        remove => state.Changed -= value;
    }

    public Task LoadAsync() => state.Loaded ? Task.CompletedTask : state.LoadAsync();

    /// <summary>Signs out at the API, forgets the person, and reloads the home page so nothing of theirs stays drawn.</summary>
    public async Task SignOutAsync()
    {
        await api.LogoutAsync();
        state.Clear();
        navigation.NavigateTo(SafeReturn.Home, forceLoad: true);
    }
}
