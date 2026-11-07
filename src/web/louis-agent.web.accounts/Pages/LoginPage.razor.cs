using LouisAgent.Web.Accounts.Client;
using LouisAgent.Web.Accounts.Client.Navigation;
using LouisAgent.Web.Accounts.Text;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Accounts.Pages;

/// <summary>The sign-in page: once signed in, the person goes back to where they came from, if that's one of ours.</summary>
public partial class LoginPage
{
    [Inject] private AccountState State { get; set; } = null!;
    [Inject] private AccountText Text { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    /// <summary>The page to go back to after signing in (<c>?returnUrl=</c>), checked before it's followed.</summary>
    [Parameter, SupplyParameterFromQuery]
    public string? ReturnUrl { get; set; }

    /// <summary>The API sends the browser here with <c>?failed=external</c> when a provider's sign-in didn't work.</summary>
    [Parameter, SupplyParameterFromQuery]
    public string? Failed { get; set; }

    /// <summary>Someone already signed in has no business here.</summary>
    protected override async Task OnInitializedAsync()
    {
        if (!State.Loaded) await State.LoadAsync();
        if (State.SignedIn) GoOn();
    }

    private void GoOn() => Navigation.NavigateTo(SafeReturn.Of(ReturnUrl));
}
