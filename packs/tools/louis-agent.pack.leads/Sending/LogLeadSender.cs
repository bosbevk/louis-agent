using LouisAgent.Composition.Logging;
using LouisAgent.Packs.Leads.Messages;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Packs.Leads.Sending;

/// <summary>
/// The sender until there's a key: the lead goes to the log, whole, so the PC and the tests send nothing and you can
/// still see what the guide would have sent.
/// </summary>
/// <remarks>
/// Null Object behind <see cref="ILeadSender"/>: the tool and the voice work end to end with no mail service, and the
/// real sender is one more class behind the same interface.
/// </remarks>
public sealed class LogLeadSender(ILogger<LogLeadSender> log) : ILeadSender
{
    /// <summary>The console's bracket for these lines.</summary>
    private const string Tag = "LEAD";

    /// <inheritdoc />
    public Task SendAsync(Lead lead, CancellationToken cancellationToken)
    {
        log.Log(LogLevel.Information, LeadsLogMessage.Logged,
            new
            {
                lead.Name,
                lead.Contact,
                lead.Wants,
                At = lead.At.ToString("u")
            }, tag: Tag);
        return Task.CompletedTask;
    }
}
