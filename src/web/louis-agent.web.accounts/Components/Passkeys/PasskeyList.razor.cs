using LouisAgent.Accounts.Contracts;
using LouisAgent.Web.Accounts.Client;
using LouisAgent.Web.Accounts.Client.Errors;
using LouisAgent.Web.Accounts.Text;
using LouisAgent.Web.Scripts;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace LouisAgent.Web.Accounts.Components.Passkeys;

/// <summary>
/// The profile's passkeys: the list the API keeps, remove, and add, which asks the API for the options, the browser
/// for the credential (<c>passkeys.create</c> in <c>passkeys.js</c>), and the API to keep it under the name given.
/// The list is read again after every change (ADR 0042).
/// </summary>
public partial class PasskeyList : IAsyncDisposable
{
    private JsModule? _passkeysModule;

    private IReadOnlyList<PasskeyInfo> _passkeys = [];
    private string _name = "";
    private AccountErrorView? _notice;
    private bool _busy, _isError, _supported;

    [Inject] private AccountApi Api { get; set; } = null!;
    [Inject] private AccountText Text { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;

    /// <summary>The library's passkeys module (CL31): the browser's WebAuthn calls.</summary>
    private JsModule Passkeys => _passkeysModule ??= new JsModule(JS, AccountScripts.Passkeys);

    /// <summary>Asks the browser whether it can do passkeys, then reads the list.</summary>
    protected override async Task OnInitializedAsync()
    {
        _supported = await Passkeys.InvokeAsync<bool>("supported");
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            _passkeys = (await Api.PasskeysAsync()).Passkeys;
        }
        catch (Exception ex) when (ex is AccountApiException or HttpRequestException)
        {
            _notice = AccountErrors.Of(ex, Text);
            _isError = true;
        }
    }

    /// <summary>The options from the API, the credential from the browser, the passkey to the API.</summary>
    private async Task AddAsync()
    {
        _busy = true;
        _notice = null;
        try
        {
            string options = await Api.PasskeyCreationOptionsAsync();
            string credential = await Passkeys.InvokeAsync<string>("create", options);
            await Api.AddPasskeyAsync(new PasskeyBody(credential, _name));
            _name = "";
            _notice = new AccountErrorView(Text.PasskeyAdded, []);
            _isError = false;
            await LoadAsync();
        }
        catch (JSException)
        {
            // The person closed the browser's dialog, or the authenticator said no.
            _notice = new AccountErrorView(Text.PasskeyCancelled, []);
            _isError = true;
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

    private async Task RemoveAsync(string id)
    {
        _busy = true;
        _notice = null;
        try
        {
            await Api.RemovePasskeyAsync(id);
            await LoadAsync();
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

    /// <summary>Lets the module go, if it was ever loaded.</summary>
    public ValueTask DisposeAsync() => _passkeysModule?.DisposeAsync() ?? ValueTask.CompletedTask;
}
