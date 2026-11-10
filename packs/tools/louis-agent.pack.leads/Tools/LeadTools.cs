using System.ComponentModel;
using LouisAgent.Composition.Messages;
using LouisAgent.Composition.Sessions;
using LouisAgent.Packs.Leads.Messages;
using LouisAgent.Packs.Leads.Sending;

namespace LouisAgent.Packs.Leads.Tools;

/// <summary>
/// The <c>leads</c> toolset (F112): one tool that tells the owner someone is interested. It refuses an incomplete
/// lead, so the guide asks for what is missing instead of sending half a message.
/// </summary>
/// <remarks>
/// A tool for a side effect, with the consent in the prompt (ADR 0080): the voice says when it may be called (after
/// the visitor's yes, once), the tool checks what it is given. A failure comes back as text with its reason, never a
/// throw into the loop (ADR 0005).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0080-a-tool-when-it-improves-the-prompt.md">ADR 0080: a tool when it improves the prompt</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0005-guard-the-context.md">ADR 0005: guard the context</seealso>
public sealed class LeadTools(ILeadSender sender, TimeProvider time)
{
    /// <summary>Sends the owner a lead: who, how to reach them, what they want.</summary>
    [Description(
        "Tells the owner what this person wants and how to reach them: a lead, a booking request, a call back. Call " +
        "it once, only after they gave their name, a way to reach them and what they want, and said yes to " +
        "sending it.")]
    public async Task<string> SendLead(
        [Description("The person's name, as they gave it.")]
        string name,
        [Description("How the owner may reach them: a mail address or a phone number.")]
        string contact,
        [Description("What they want, in their own words, with any day or time they asked for.")]
        string wants,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name)) return MessageText.Format(LeadsToolMessage.NoName);
        if (string.IsNullOrWhiteSpace(contact)) return MessageText.Format(LeadsToolMessage.NoContact);
        if (string.IsNullOrWhiteSpace(wants)) return MessageText.Format(LeadsToolMessage.NoWants);

        // Who it's for (F127.2): the identity the turn runs as, and whether that's a demonstration, so a try-out's
        // booking is marked a demo; outside a turn, no one's and no demo.
        var who = TurnIdentity.Current;
        var lead = new Lead(name.Trim(), contact.Trim(), wants.Trim(), time.GetUtcNow(), who?.Name,
            who?.IsDemonstration == true);
        try
        {
            await sender.SendAsync(lead, cancellationToken);
        }
        catch (Exception failure) when (failure is HttpRequestException or InvalidOperationException)
        {
            // The service said no, or couldn't be reached: the model hears why and tells the visitor the other way in.
            return MessageText.Format(LeadsToolMessage.Failed, new { Reason = failure.Message });
        }

        // The truth to the model: a lead that only went to the log was not mailed to anyone, and a demonstration's
        // went to no real owner (F127.2), so the visitor hears that from it.
        bool demo = lead.Demonstration;
        if (!sender.Mails)
        {
            return MessageText.Format(demo ? LeadsToolMessage.LoggedDemo : LeadsToolMessage.Logged,
                new
                {
                    lead.Name,
                    lead.Contact,
                    lead.Wants
                });
        }

        return MessageText.Format(demo ? LeadsToolMessage.SentDemo : LeadsToolMessage.Sent,
            new
            {
                At = lead.At.ToString("u"),
                lead.Name,
                lead.Contact,
                lead.Wants
            });
    }
}
