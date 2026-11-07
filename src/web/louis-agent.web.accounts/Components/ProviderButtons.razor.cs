using LouisAgent.Accounts.Contracts;
using LouisAgent.Choices;
using LouisAgent.Web.Accounts.Client;
using LouisAgent.Web.Accounts.Client.Navigation;
using LouisAgent.Web.Accounts.Components.Providers;
using LouisAgent.Web.Accounts.Text;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Accounts.Components;

/// <summary>The sign-in providers this instance has keys for, as buttons; read from the API when the page opens.</summary>
public partial class ProviderButtons
{
    private IReadOnlyList<ProviderInfo> _providers = [];

    [Inject] private AccountApi Api { get; set; } = null!;
    [Inject] private AccountText Text { get; set; } = null!;

    /// <summary>Where the person goes after the provider sends them back.</summary>
    [Parameter]
    public string? ReturnUrl { get; set; }

    /// <summary>Asks the API which providers are on; a failure shows none.</summary>
    protected override async Task OnInitializedAsync()
    {
        try
        {
            _providers = (await Api.ProvidersAsync()).Providers.Where(p => p.Enabled).ToList();
        }
        catch (Exception ex) when (ex is AccountApiException or HttpRequestException)
        {
            _providers = [];
        }
    }

    /// <summary>The API's challenge route for this provider, with a safe place to come back to.</summary>
    private string LinkFor(string provider) =>
        $"account/external/{Uri.EscapeDataString(provider)}?returnUrl={Uri.EscapeDataString(SafeReturn.Of(ReturnUrl))}";

    /// <summary>
    /// The button's classes (FX66): the brand's own look, which its guidelines fix, or the site's plain button for a
    /// provider we have no look for.
    /// </summary>
    private static string Look(ProviderBrand? brand) =>
        brand is { } known ? $"provider-button is-{known.Wire()}" : "account-button is-plain";
}
