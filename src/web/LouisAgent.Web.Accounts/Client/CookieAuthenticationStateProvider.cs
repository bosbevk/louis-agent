using System.Security.Claims;
using LouisAgent.Accounts.Contracts;
using Microsoft.AspNetCore.Components.Authorization;

namespace LouisAgent.Web.Accounts.Client;

/// <summary>
/// Blazor's view of who is signed in, built from <see cref="AccountState"/>, so <c>AuthorizeView</c> and
/// <c>[Authorize]</c> work on the pages. The cookie itself never reaches the app; the person comes from the API.
/// </summary>
/// <remarks>
/// Adapter: Blazor wants an <see cref="AuthenticationStateProvider"/>, the app keeps one state. The state is the
/// truth and this follows it, so a sign-in in one place is seen everywhere without a second read.
/// </remarks>
public sealed class CookieAuthenticationStateProvider : AuthenticationStateProvider, IDisposable
{
    private readonly AccountState _state;

    public CookieAuthenticationStateProvider(AccountState state)
    {
        _state = state;
        _state.Changed += Announce;
    }

    /// <summary>The person the state holds, as claims; a guest has none.</summary>
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (!_state.Loaded) await _state.LoadAsync();
        return new AuthenticationState(PrincipalOf(_state.Me));
    }

    /// <summary>Claims as the server's cookie carries them, so a page reads the same names on both sides.</summary>
    public static ClaimsPrincipal PrincipalOf(AccountInfo? me)
    {
        if (me is null) return new ClaimsPrincipal(new ClaimsIdentity());
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, me.UserId),
            new Claim(ClaimTypes.Name, me.DisplayName ?? me.Email),
            new Claim(ClaimTypes.Email, me.Email),
            new Claim(AccountClaims.Tenant, me.TenantId)
        ], authenticationType: "cookie");
        return new ClaimsPrincipal(identity);
    }

    private void Announce() =>
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(PrincipalOf(_state.Me))));

    public void Dispose() => _state.Changed -= Announce;
}
