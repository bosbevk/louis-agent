using LouisAgent.Accounts.Contracts;
using LouisAgent.Web.Accounts.Client;
using LouisAgent.Web.Accounts.Client.Errors;
using LouisAgent.Web.Accounts.Text;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Accounts.Pages;

/// <summary>
/// Sets a new password with the mailed code: the two passwords must match here, Identity checks the code and its
/// rules, and what didn't pass is listed.
/// </summary>
public partial class ResetPasswordPage
{
    private string _email = "", _code = "", _password = "", _again = "";
    private AccountErrorView? _error;
    private bool _busy, _done;

    [Inject] private AccountApi Api { get; set; } = null!;
    [Inject] private AccountText Text { get; set; } = null!;

    /// <summary>The email the forgot page sent along, so it isn't typed twice.</summary>
    [Parameter, SupplyParameterFromQuery]
    public string? Email { get; set; }

    protected override void OnParametersSet()
    {
        if (_email.Length == 0 && Email is not null) _email = Email;
    }

    /// <summary>Checks the passwords agree, then resets; a bad code or a weak password is listed.</summary>
    private async Task ResetAsync()
    {
        _error = null;
        if (_password != _again)
        {
            _error = new AccountErrorView(Text.PasswordsDiffer, []);
            return;
        }

        _busy = true;
        try
        {
            await Api.ResetPasswordAsync(new ResetPasswordBody(_email.Trim(), _code.Trim(), _password));
            _done = true;
        }
        catch (Exception ex) when (ex is AccountApiException or HttpRequestException)
        {
            _error = AccountErrors.Of(ex, Text);
        }
        finally
        {
            _busy = false;
        }
    }
}
