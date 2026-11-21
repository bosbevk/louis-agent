using LouisAgent.Accounts.Contracts;
using LouisAgent.Web.Accounts.Client;
using LouisAgent.Web.Accounts.Client.Errors;
using LouisAgent.Web.Accounts.Text;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Accounts.Components.Profile;

/// <summary>
/// The providers linked to the account, with unlink, and the ones that could be: linking leaves the app for the API's
/// challenge route and comes back to the profile; unlinking is a call, and the state takes the account after.
/// </summary>
public partial class LinkedLogins
{
    private IReadOnlyList<ProviderInfo> _providers = [];
    private AccountErrorView? _notice;
    private bool _busy;

    [Inject] private AccountApi Api { get; set; } = null!;
    [Inject] private AccountState State { get; set; } = null!;
    [Inject] private AccountText Text { get; set; } = null!;

    /// <summary>The signed-in person.</summary>
    [Parameter, EditorRequired]
    public AccountInfo Me { get; set; } = null!;

    /// <summary>The linked providers, labelled; one the instance no longer knows keeps its wire name as its label.</summary>
    private IEnumerable<(string Name, string Label)> Linked =>
        Me.Logins.Select(name => (name, _providers.FirstOrDefault(p => p.Name == name)?.Label ?? name));

    /// <summary>The providers with keys that aren't on the account yet.</summary>
    private IReadOnlyList<ProviderInfo> Unlinked =>
        _providers.Where(p => p.Enabled && !Me.Logins.Contains(p.Name)).ToList();

    /// <summary>Reads which providers this instance has; a failure shows none to link.</summary>
    protected override async Task OnInitializedAsync()
    {
        try
        {
            _providers = (await Api.ProvidersAsync()).Providers;
        }
        catch (Exception ex) when (ex is AccountApiException or HttpRequestException)
        {
            _providers = [];
        }
    }

    /// <summary>The API's challenge route, linking, back to the profile.</summary>
    private static string LinkFor(string provider) =>
        $"account/external/{Uri.EscapeDataString(provider)}?link=true&returnUrl={AccountPages.Profile}";

    private async Task UnlinkAsync(string provider)
    {
        _busy = true;
        _notice = null;
        try
        {
            await Api.UnlinkAsync(provider);
            State.Set(await Api.MeAsync());
        }
        catch (Exception ex) when (ex is AccountApiException or HttpRequestException)
        {
            _notice = AccountErrors.Of(ex, Text);
        }
        finally
        {
            _busy = false;
        }
    }
}
