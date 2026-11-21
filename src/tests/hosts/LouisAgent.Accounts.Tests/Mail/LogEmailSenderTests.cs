using LouisAgent.Accounts.Mail;
using LouisAgent.Accounts.Users;
using LouisAgent.Composition.Logging.Content;

namespace LouisAgent.Accounts.Tests.Mail;

/// <summary>The log is the mail: a link is written in full on a developer's machine and hashed anywhere else.</summary>
public class LogEmailSenderTests
{
    private const string Link = "https://example.test/account/confirmEmail?userId=1&code=abc";
    private static readonly AccountUser Ann = AccountUser.New("home", "ann@example.com", DateTimeOffset.UnixEpoch);

    /// <summary>Runs the sender with the log in this mode, then puts the process-wide mode back.</summary>
    private static async Task<string> LineIn(LogContentMode mode, Func<LogEmailSender, Task> send)
    {
        var log = new CapturingLogger<LogEmailSender>();
        LogContent.Use(mode);
        try
        {
            await send(new LogEmailSender(log));
        }
        finally
        {
            LogContent.Use(LogContentMode.Hashed);
        }

        return log.Lines.Single();
    }

    [Test]
    public async Task InFullMode_TheConfirmationLink_IsInTheLine()
    {
        string line = await LineIn(LogContentMode.Full, s => s.SendConfirmationLinkAsync(Ann, Ann.Email!, Link));

        Assert.That(line, Does.StartWith("Mail to ann@example.com: confirm the account at " + Link));
    }

    [Test]
    public async Task InHashedMode_TheLineHoldsAHash_NotTheLink()
    {
        string line = await LineIn(LogContentMode.Hashed, s => s.SendPasswordResetLinkAsync(Ann, Ann.Email!, Link));

        Assert.Multiple(() =>
        {
            Assert.That(line, Does.Not.Contain("example.test").And.Not.Contain("ann@"));
            Assert.That(line, Does.Contain("reset the password at #"));
        });
    }

    /// <summary>Identity hands over a link made for an HTML mail; the log gets one a person can click.</summary>
    [Test]
    public async Task ALinkMadeForHtml_IsLoggedPlain()
    {
        string line = await LineIn(LogContentMode.Full,
            s => s.SendConfirmationLinkAsync(Ann, Ann.Email!, "https://x.test/confirm?userId=1&amp;code=abc"));

        Assert.That(line, Does.Contain("https://x.test/confirm?userId=1&code=abc"));
    }

    [Test]
    public async Task AResetCode_IsLoggedTheSameWay()
    {
        string line = await LineIn(LogContentMode.Full, s => s.SendPasswordResetCodeAsync(Ann, Ann.Email!, "123456"));

        Assert.That(line, Does.Contain("the password reset code is 123456"));
    }
}
