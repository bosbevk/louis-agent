using LouisAgent.Accounts.Contracts;
using LouisAgent.Web.Accounts.Client;
using LouisAgent.Web.Accounts.Text;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Accounts.Pages;

/// <summary>Asks the API to mail a reset code, then points at the reset page with the email filled in.</summary>
public partial class ForgotPasswordPage
{
    private string _email = "";
    private string? _error;
    private bool _busy, _sent;

    [Inject] private AccountApi Api { get; set; } = null!;
    [Inject] private AccountText Text { get; set; } = null!;

    /// <summary>The reset page, with the email in its query so it isn't typed twice.</summary>
    private string ResetLink => $"{AccountPages.ResetPassword}?email={Uri.EscapeDataString(_email.Trim())}";

    /// <summary>Sends the email; the API says the same either way, and so does the page.</summary>
    private async Task SendAsync()
    {
        _busy = true;
        _error = null;
        try
        {
            await Api.ForgotPasswordAsync(new ForgotPasswordBody(_email.Trim()));
            _sent = true;
        }
        catch (Exception ex) when (ex is AccountApiException or HttpRequestException)
        {
            _error = Text.SomethingWentWrong;
        }
        finally
        {
            _busy = false;
        }
    }
}
