using LouisAgent.Accounts.Contracts;
using LouisAgent.Web.Accounts.Client;
using LouisAgent.Web.Accounts.Client.Navigation;
using LouisAgent.Web.Accounts.Text;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Accounts.Pages;

/// <summary>
/// The profile page: it draws whoever the state says is signed in, redraws when that changes, and sends a guest to
/// sign in with this page as the way back.
/// </summary>
public partial class ProfilePage
{
    [Inject] private AccountApi Api { get; set; } = null!;
    [Inject] private AccountState State { get; set; } = null!;
    [Inject] private AccountText Text { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    /// <summary>The API sends the browser back here with <c>?failed=link</c> when a provider couldn't be linked.</summary>
    [Parameter, SupplyParameterFromQuery]
    public string? Failed { get; set; }

    /// <summary>Reads who is signed in; a guest goes to the sign-in page and comes back here.</summary>
    protected override async Task OnInitializedAsync()
    {
        State.Changed += StateHasChanged;
        if (!State.Loaded) await State.LoadAsync();
        if (!State.SignedIn)
            Navigation.NavigateTo($"{AccountPages.Login}?returnUrl={Uri.EscapeDataString(AccountPages.Profile)}");
    }

    private async Task SignOutAsync()
    {
        await Api.LogoutAsync();
        State.Clear();
        GoHome();
    }

    private void GoHome() => Navigation.NavigateTo(SafeReturn.Home);

    public void Dispose() => State.Changed -= StateHasChanged;
}
