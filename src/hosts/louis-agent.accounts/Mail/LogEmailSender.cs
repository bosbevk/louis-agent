using LouisAgent.Accounts.Messages;
using LouisAgent.Accounts.Users;
using LouisAgent.Composition.Logging;
using LouisAgent.Composition.Logging.Content;
using Microsoft.AspNetCore.Identity;

namespace LouisAgent.Accounts.Mail;

/// <summary>
/// The mail sender until there's a real one: the confirmation link, the reset link or the reset code goes to the log,
/// as the log shows values (<c>LOG_CONTENT=full</c> to read it on your own machine; hashed otherwise, so a hosted
/// instance never writes a link anyone could use).
/// </summary>
/// <remarks>
/// Null Object behind Identity's own seam, <see cref="IEmailSender{TUser}"/> (Louis's pick A, F61): registration and
/// reset work end to end today, and a real sender (a mail service, once picked) is one more class behind the same
/// interface, with nothing else to change.
/// </remarks>
public sealed class LogEmailSender(ILogger<LogEmailSender> log) : IEmailSender<AccountUser>
{
    /// <summary>The console's bracket for these lines.</summary>
    private const string Tag = "MAIL";

    /// <summary>The link that confirms a new account's email.</summary>
    public Task SendConfirmationLinkAsync(AccountUser user, string email, string confirmationLink)
    {
        log.Log(LogLevel.Information, AccountLogMessage.ConfirmationLink,
            new
            {
                Email = LogContent.Of(email),
                Link = LogContent.Of(confirmationLink)
            }, tag: Tag);
        return Task.CompletedTask;
    }

    /// <summary>The link that opens the password reset page.</summary>
    public Task SendPasswordResetLinkAsync(AccountUser user, string email, string resetLink)
    {
        log.Log(LogLevel.Information, AccountLogMessage.PasswordResetLink,
            new
            {
                Email = LogContent.Of(email),
                Link = LogContent.Of(resetLink)
            }, tag: Tag);
        return Task.CompletedTask;
    }

    /// <summary>The code Identity's API hands out instead of a link, when a client asks for one.</summary>
    public Task SendPasswordResetCodeAsync(AccountUser user, string email, string resetCode)
    {
        log.Log(LogLevel.Information, AccountLogMessage.PasswordResetCode,
            new
            {
                Email = LogContent.Of(email),
                Code = LogContent.Of(resetCode)
            }, tag: Tag);
        return Task.CompletedTask;
    }
}
