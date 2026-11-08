namespace LouisAgent.Packs.Leads.Sending;

/// <summary>
/// How a lead reaches the owner. The tool talks to this, so how the mail goes out (a mail service, the log on the PC)
/// is one class behind it and the tool never changes.
/// </summary>
/// <remarks>
/// Strategy, with a Null Object: <see cref="LogLeadSender"/> is what runs with no key and in every test, so nothing
/// is ever sent by accident, and the real sender is picked by the pack's options. <see cref="Mails"/> lets the tool
/// tell the model the truth: a lead that only went to the log was not mailed to anyone.
/// </remarks>
public interface ILeadSender
{
    /// <summary>Whether a lead handed to this sender reaches the owner's mailbox.</summary>
    bool Mails { get; }

    /// <summary>Sends the lead. Throws when it couldn't, with the reason; the tool turns that into text for the model.</summary>
    Task SendAsync(Lead lead, CancellationToken cancellationToken);
}
