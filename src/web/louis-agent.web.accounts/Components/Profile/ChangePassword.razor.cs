using LouisAgent.Accounts.Contracts;
using LouisAgent.Web.Accounts.Client;
using LouisAgent.Web.Accounts.Client.Errors;
using LouisAgent.Web.Accounts.Text;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Accounts.Components.Profile;

/// <summary>Changes the password through Identity's info route, which wants the current one too.</summary>
public partial class ChangePassword
{
    private string _current = "", _password = "", _again = "";
    private AccountErrorView? _notice;
    private bool _busy, _isError;

    [Inject] private AccountApi Api { get; set; } = null!;
    [Inject] private AccountText Text { get; set; } = null!;

    /// <summary>The signed-in person; only one with a password sees this section.</summary>
    [Parameter, EditorRequired]
    public AccountInfo Me { get; set; } = null!;

    private async Task ChangeAsync()
    {
        _notice = null;
        if (_password != _again)
        {
            _notice = new AccountErrorView(Text.PasswordsDiffer, []);
            _isError = true;
            return;
        }

        _busy = true;
        try
        {
            await Api.ChangeInfoAsync(new InfoBody(NewPassword: _password, OldPassword: _current));
            _notice = new AccountErrorView(Text.PasswordChanged, []);
            _isError = false;
            _current = _password = _again = "";
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
