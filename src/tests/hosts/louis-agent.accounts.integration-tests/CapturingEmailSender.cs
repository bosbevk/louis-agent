using System.Net;
using LouisAgent.Accounts.Users;
using Microsoft.AspNetCore.Identity;

namespace LouisAgent.Accounts.IntegrationTests;

/// <summary>
/// The mail seam for a test: every link and code is kept, so the test can follow it. Identity writes a link for an
/// HTML mail, so it's decoded as a mail reader would.
/// </summary>
internal sealed class CapturingEmailSender : IEmailSender<AccountUser>
{
    public List<string> Links { get; } = [];
    public List<string> Codes { get; } = [];

    public Task SendConfirmationLinkAsync(AccountUser user, string email, string confirmationLink)
    {
        Links.Add(WebUtility.HtmlDecode(confirmationLink));
        return Task.CompletedTask;
    }

    public Task SendPasswordResetLinkAsync(AccountUser user, string email, string resetLink)
    {
        Links.Add(WebUtility.HtmlDecode(resetLink));
        return Task.CompletedTask;
    }

    public Task SendPasswordResetCodeAsync(AccountUser user, string email, string resetCode)
    {
        Codes.Add(resetCode);
        return Task.CompletedTask;
    }
}
