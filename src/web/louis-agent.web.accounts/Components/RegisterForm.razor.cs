using LouisAgent.Accounts.Contracts;
using LouisAgent.Web.Accounts.Client;
using LouisAgent.Web.Accounts.Client.Errors;
using LouisAgent.Web.Accounts.Text;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Accounts.Components;

/// <summary>
/// Makes an account: the two passwords must match here, Identity's rules are checked on the server, and the page that
/// holds the form hears which email was registered, to say where the confirmation link went.
/// </summary>
public partial class RegisterForm
{
    private string _email = "", _password = "", _again = "";
    private bool _busy;
    private AccountErrorView? _error;

    [Inject] private AccountApi Api { get; set; } = null!;
    [Inject] private AccountText Text { get; set; } = null!;

    /// <summary>Fires with the email once the account is made and its confirmation mail sent.</summary>
    [Parameter]
    public EventCallback<string> OnRegistered { get; set; }

    /// <summary>Checks the passwords agree, then registers; what didn't pass is listed.</summary>
    private async Task RegisterAsync()
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
            string email = _email.Trim();
            await Api.RegisterAsync(new RegisterBody(email, _password));
            await OnRegistered.InvokeAsync(email);
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
