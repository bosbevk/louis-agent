using LouisAgent.Accounts.Contracts;
using LouisAgent.Web.Accounts.Client;
using LouisAgent.Web.Accounts.Client.Errors;
using LouisAgent.Web.Accounts.Text;
using LouisAgent.Web.Scripts;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace LouisAgent.Web.Accounts.Components.Passkeys;

/// <summary>
/// Signs in with a passkey: the options from the API (any passkey, since no email is asked for), the credential from
/// the browser (<c>passkeys.get</c>), the sign-in from the API, and the state takes who it is.
/// </summary>
public partial class PasskeySignIn : IAsyncDisposable
{
    private JsModule? _passkeysModule;

    private AccountErrorView? _error;
    private bool _busy, _supported;

    [Inject] private AccountApi Api { get; set; } = null!;
    [Inject] private AccountState State { get; set; } = null!;
    [Inject] private AccountText Text { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;

    /// <summary>The library's passkeys module (CL31): the browser's WebAuthn calls.</summary>
    private JsModule Passkeys => _passkeysModule ??= new JsModule(JS, AccountScripts.Passkeys);

    /// <summary>Fires once the person is signed in and the state knows them.</summary>
    [Parameter]
    public EventCallback OnSignedIn { get; set; }

    protected override async Task OnInitializedAsync() => _supported = await Passkeys.InvokeAsync<bool>("supported");

    private async Task SignInAsync()
    {
        _busy = true;
        _error = null;
        try
        {
            string options = await Api.PasskeyRequestOptionsAsync(null);
            string credential = await Passkeys.InvokeAsync<string>("get", options);
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

    /// <summary>Lets the module go, if it was ever loaded.</summary>
    public ValueTask DisposeAsync() => _passkeysModule?.DisposeAsync() ?? ValueTask.CompletedTask;
}
