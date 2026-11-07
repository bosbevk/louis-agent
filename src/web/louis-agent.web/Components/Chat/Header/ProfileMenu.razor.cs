using LouisAgent.Web.Accounts;
using LouisAgent.Web.Accounts.Navigation;
using LouisAgent.Web.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Components.Chat.Header;

/// <summary>
/// The profile's menu: the signed-in person with their profile and sign out, a guest with sign in and register, or,
/// on an instance with no accounts, the guest's menu as it always was.
/// </summary>
/// <remarks>
/// The account services are looked up, not injected: a site that hasn't registered them (and every test of the chat
/// that didn't) still draws the menu, as a guest. That's the one place the framework's web asks for a service by
/// hand, and the reason is that accounts are a site's choice, not the chat's.
/// </remarks>
public partial class ProfileMenu : IDisposable
{
    private AccountState? _state;
    private AccountApi? _api;
    private AccountText _words = new();

    [Inject] private ChatText Text { get; set; } = null!;
    [Inject] private IServiceProvider Services { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    [Parameter] public EventCallback<string> OnComingSoon { get; set; }

    /// <summary>Whether this instance has accounts: the services are registered and the API has them.</summary>
    private bool Accounts => _state?.Enabled == true;

    private bool SignedIn => _state?.SignedIn == true;

    /// <summary>The site's words for sign in and register.</summary>
    private AccountText Words => _words;

    /// <summary>The first letter of the person's name, or the guest's.</summary>
    private string Initial =>
        SignedIn ? (_state!.Me!.DisplayName ?? _state.Me.Email)[..1].ToUpperInvariant() : Text.GuestInitial;

    private string Who => SignedIn ? _state!.Me!.DisplayName ?? _state.Me.Email : Text.Guest;

    private string Detail => SignedIn ? _state!.Me!.Email : Text.LocalOnly;

    /// <summary>Finds the account services if the site has them, and follows the state from then on.</summary>
    protected override async Task OnInitializedAsync()
    {
        _state = Services.GetService<AccountState>();
        _api = Services.GetService<AccountApi>();
        _words = Services.GetService<AccountText>() ?? _words;
        if (_state is null) return;
        _state.Changed += StateHasChanged;
        if (!_state.Loaded) await _state.LoadAsync();
    }

    private async Task SignOutAsync()
    {
        if (_api is null || _state is null) return;
        await _api.LogoutAsync();
        _state.Clear();
        Navigation.NavigateTo(SafeReturn.Home, forceLoad: true);
    }

    public void Dispose()
    {
        if (_state is not null) _state.Changed -= StateHasChanged;
    }
}
