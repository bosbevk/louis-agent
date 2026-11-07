using LouisAgent.Accounts.Contracts;
using LouisAgent.Web.Accounts;
using LouisAgent.Web.Accounts.Errors;
using LouisAgent.Web.Text;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Account;

/// <summary>
/// Signs a person in with the cookie: the API checks the email and the password, the state takes who they are, and
/// the page that holds the form hears it's done. A refusal shows why, in the site's words.
/// </summary>
public partial class LoginForm
{
    private string _email = "", _password = "";
    private bool _busy;
    private AccountErrorView? _error;

    [Inject] private AccountApi Api { get; set; } = null!;
    [Inject] private AccountState State { get; set; } = null!;
    [Inject] private AccountText Text { get; set; } = null!;

    /// <summary>Fires once the person is signed in and the state knows them.</summary>
    [Parameter]
    public EventCallback OnSignedIn { get; set; }

    /// <summary>Sends the two fields; on a yes reads who signed in, on a no says why.</summary>
    private async Task SignInAsync()
    {
        _busy = true;
        _error = null;
        try
        {
            await Api.LoginAsync(new LoginBody(_email.Trim(), _password));
            State.Set(await Api.MeAsync());
            await OnSignedIn.InvokeAsync();
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
