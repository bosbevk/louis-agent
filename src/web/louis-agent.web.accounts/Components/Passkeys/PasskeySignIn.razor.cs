using LouisAgent.Accounts.Contracts;
using LouisAgent.Web.Accounts.Client;
using LouisAgent.Web.Accounts.Client.Errors;
using LouisAgent.Web.Accounts.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace LouisAgent.Web.Accounts.Components.Passkeys;

/// <summary>
/// Signs in with a passkey: the options from the API (any passkey, since no email is asked for), the credential from
/// the browser (<c>passkeys.get</c>), the sign-in from the API, and the state takes who it is.
/// </summary>
public partial class PasskeySignIn
{
    private AccountErrorView? _error;
    private bool _busy, _supported;

    [Inject] private AccountApi Api { get; set; } = null!;
    [Inject] private AccountState State { get; set; } = null!;
    [Inject] private AccountText Text { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;

    /// <summary>Fires once the person is signed in and the state knows them.</summary>
    [Parameter]
    public EventCallback OnSignedIn { get; set; }

    protected override async Task OnInitializedAsync() => _supported = await JS.InvokeAsync<bool>("passkeys.supported");

    private async Task SignInAsync()
    {
        _busy = true;
        _error = null;
        try
        {
            string options = await Api.PasskeyRequestOptionsAsync(null);
            string credential = await JS.InvokeAsync<string>("passkeys.get", options);
            await Api.PasskeyLoginAsync(new PasskeyLoginBody(credential));
            State.Set(await Api.MeAsync());
            await OnSignedIn.InvokeAsync();
        }
        catch (JSException)
        {
            _error = new AccountErrorView(Text.PasskeyCancelled, []);
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
