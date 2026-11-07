using LouisAgent.Web.Components.Chat.Header.Menu;
using LouisAgent.Web.Text;
using LouisAgent.Web.Scripts;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Components.Chat.Header;

/// <summary>
/// The profile's menu: the signed-in person with their profile and sign out, a guest with sign in and register, or,
/// on an instance with no accounts, the guest's menu as it always was. Everything it knows of accounts comes through
/// <see cref="IAccountMenu"/> (CL30).
/// </summary>
/// <remarks>
/// The seam is asked for once and falls back to <see cref="NoAccounts"/>, a Null Object: a site that registered no
/// accounts (and every test of the chat that didn't) draws the menu as a guest's, with no branch here for the case.
/// </remarks>
public partial class ProfileMenu : IAsyncDisposable
{
    private JsModule? _shell;

    [Inject] private IJSRuntime JS { get; set; } = null!;

    /// <summary>The shell's module (CL31), shared with every part of the chat; the browser fetches it once.</summary>
    private JsModule Shell => _shell ??= new JsModule(JS, FrameworkScripts.Shell);

    private IAccountMenu _menu = NoAccounts.Instance;

    [Inject] private ChatText Text { get; set; } = null!;
    [Inject] private IServiceProvider Services { get; set; } = null!;

    [Parameter] public EventCallback<string> OnComingSoon { get; set; }

    private bool Accounts => _menu.Enabled;

    private bool SignedIn => _menu.SignedIn;

    /// <summary>The first letter of the person's name, or the guest's.</summary>
    private string Initial => SignedIn ? _menu.Name![..1].ToUpperInvariant() : Text.GuestInitial;

    private string Who => SignedIn ? _menu.Name! : Text.Guest;

    private string Detail => SignedIn ? _menu.Email! : Text.LocalOnly;

    /// <summary>Takes the site's accounts if it has them, and follows them from then on.</summary>
    protected override async Task OnInitializedAsync()
    {
        _menu = Services.GetService<IAccountMenu>() ?? NoAccounts.Instance;
        _menu.Changed += StateHasChanged;
        await _menu.LoadAsync();
    }

    private Task SignOutAsync() => _menu.SignOutAsync();

    public ValueTask DisposeAsync()
    {
        _menu.Changed -= StateHasChanged;
        return _shell?.DisposeAsync() ?? ValueTask.CompletedTask;
    }

    /// <summary>Shows the sidebar if it's hidden, then opens its settings.</summary>
    private Task OpenSettings() => Shell.InvokeVoidAsync("openSettings").AsTask();
}
