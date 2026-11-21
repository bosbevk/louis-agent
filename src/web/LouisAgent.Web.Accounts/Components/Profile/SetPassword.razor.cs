using LouisAgent.Accounts.Contracts;
using LouisAgent.Web.Accounts.Client;
using LouisAgent.Web.Accounts.Client.Errors;
using LouisAgent.Web.Accounts.Text;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Accounts.Components.Profile;

/// <summary>Gives an account that signs in only through a provider a password of its own; the state takes the account after.</summary>
public partial class SetPassword
{
    private string _password = "", _again = "";
    private AccountErrorView? _notice;
    private bool _busy, _isError;

    [Inject] private AccountApi Api { get; set; } = null!;
    [Inject] private AccountState State { get; set; } = null!;
    [Inject] private AccountText Text { get; set; } = null!;

    /// <summary>The signed-in person; only one without a password sees this section.</summary>
    [Parameter, EditorRequired]
    public AccountInfo Me { get; set; } = null!;

    private async Task SetAsync()
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
            await Api.SetPasswordAsync(new SetPasswordBody(_password));
            State.Set(await Api.MeAsync());
            _notice = new AccountErrorView(Text.PasswordSet, []);
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
