using LouisAgent.Accounts.Contracts;
using LouisAgent.Web.Accounts.Client;
using LouisAgent.Web.Accounts.Text;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Accounts.Pages;

/// <summary>
/// The registration page: the form until an account is made, then where the confirmation link went, with a button
/// to send it again. The API says the same whether or not the address has an account, and so does the page.
/// </summary>
public partial class RegisterPage
{
    private string? _registered, _resent;
    private bool _busy;

    [Inject] private AccountApi Api { get; set; } = null!;
    [Inject] private AccountText Text { get; set; } = null!;

    private void Registered(string email) => _registered = email;

    /// <summary>Asks for the confirmation mail again; the answer is the same either way, on purpose.</summary>
    private async Task ResendAsync()
    {
        _busy = true;
        try
        {
            await Api.ResendConfirmationAsync(new ResendConfirmationBody(_registered!));
            _resent = Text.ConfirmationResent;
        }
        catch (Exception ex) when (ex is AccountApiException or HttpRequestException)
        {
            _resent = Text.SomethingWentWrong;
        }
        finally
        {
            _busy = false;
        }
    }
}
