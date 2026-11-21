using LouisAgent.Accounts.Contracts;
using LouisAgent.Web.Accounts.Client;
using LouisAgent.Web.Accounts.Client.Errors;
using LouisAgent.Web.Accounts.Client.TwoFactor;
using LouisAgent.Web.Accounts.Text;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Accounts.Components;

/// <summary>
/// Signs a person in with the cookie: the API checks the email and the password, the state takes who they are, and
/// the page that holds the form hears it's done. A refusal shows why, in the site's words; "RequiresTwoFactor" is
/// not a refusal but the second step, so the code box opens and the same form sends again with the code (F61.5).
/// </summary>
public partial class LoginForm
{
    private string _email = "", _password = "", _code = "";
    private bool _busy, _needsCode;
    private AccountErrorView? _error;

    [Inject] private AccountApi Api { get; set; } = null!;
    [Inject] private AccountState State { get; set; } = null!;
    [Inject] private AccountText Text { get; set; } = null!;

    /// <summary>Fires once the person is signed in and the state knows them.</summary>
    [Parameter]
    public EventCallback OnSignedIn { get; set; }

    /// <summary>Sends the fields; on a yes reads who signed in, on a no says why.</summary>
    private async Task SignInAsync()
    {
        _busy = true;
        _error = null;
        try
        {
            await Api.LoginAsync(Body());
            State.Set(await Api.MeAsync());
            await OnSignedIn.InvokeAsync();
        }
        catch (AccountApiException ex) when (ex.Problem.Detail == "RequiresTwoFactor")
        {
            // Not a no: the password passed, and the account wants the app's code too.
            _needsCode = true;
            _error = AccountErrors.Of(ex, Text);
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

    private LoginBody Body() => _needsCode
        ? SecondStep.Of(_email.Trim(), _password, _code)
        : new LoginBody(_email.Trim(), _password);
}
