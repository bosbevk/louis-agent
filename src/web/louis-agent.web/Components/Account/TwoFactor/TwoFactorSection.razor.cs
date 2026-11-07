using LouisAgent.Accounts.Contracts;
using LouisAgent.Web.Accounts;
using LouisAgent.Web.Accounts.Errors;
using LouisAgent.Web.Text;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Account.TwoFactor;

/// <summary>
/// The profile's two-factor section over Identity's one route: an empty body reads the state, the app's first
/// code turns it on, and every change shows what the API answered (ADR 0042). The issuer in the app is the site's
/// host, read from the address bar.
/// </summary>
public partial class TwoFactorSection
{
    private TwoFactorReply? _state;
    private string _code = "";
    private AccountErrorView? _notice;
    private bool _busy, _isError;

    [Inject] private AccountApi Api { get; set; } = null!;
    [Inject] private AccountText Text { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    /// <summary>Who is signed in: the email goes in the app's label.</summary>
    [Parameter, EditorRequired]
    public AccountInfo Me { get; set; } = null!;

    private string Issuer => new Uri(Navigation.BaseUri).Host;

    protected override Task OnInitializedAsync() => SendAsync(new TwoFactorBody(), null);

    private Task TurnOnAsync() =>
        SendAsync(new TwoFactorBody(Enable: true, TwoFactorCode: _code.Trim()), Text.TwoFactorOn);

    private Task TurnOffAsync() => SendAsync(new TwoFactorBody(Enable: false), Text.TwoFactorOff);

    private Task NewCodesAsync() => SendAsync(new TwoFactorBody(ResetRecoveryCodes: true), null);

    /// <summary>One call for every change: the reply is the new state, and the line to say when it worked.</summary>
    private async Task SendAsync(TwoFactorBody body, string? done)
    {
        _busy = true;
        _notice = null;
        try
        {
            _state = await Api.TwoFactorAsync(body);
            _code = "";
            _notice = done is null ? null : new AccountErrorView(done, []);
            _isError = false;
        }
        catch (Exception ex) when (ex is AccountApiException or HttpRequestException)
        {
            _notice = AccountErrors.Of(ex, Text);
            _isError = true;
        }
        finally
        {
            _busy = false;
        }
    }
}
