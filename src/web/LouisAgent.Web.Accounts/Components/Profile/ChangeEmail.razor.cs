using LouisAgent.Accounts.Contracts;
using LouisAgent.Web.Accounts.Client;
using LouisAgent.Web.Accounts.Client.Errors;
using LouisAgent.Web.Accounts.Text;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Accounts.Components.Profile;

/// <summary>Asks Identity to mail a link to the new address; the email changes only once that link is opened.</summary>
public partial class ChangeEmail
{
    private string _email = "";
    private AccountErrorView? _notice;
    private bool _busy, _isError;

    [Inject] private AccountApi Api { get; set; } = null!;
    [Inject] private AccountText Text { get; set; } = null!;

    private async Task SendAsync()
    {
        _busy = true;
        _notice = null;
        try
        {
            string email = _email.Trim();
            await Api.ChangeInfoAsync(new InfoBody(NewEmail: email));
            _notice = new AccountErrorView(string.Format(Text.EmailChangeSent, email), []);
            _isError = false;
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
}
