using LouisAgent.Composition.Messages;
using LouisAgent.Packs.Leads.Messages;

namespace LouisAgent.Packs.Leads.Sending;

/// <summary>
/// One mail as Resend's API takes it (<c>POST /emails</c>): the sender, the recipients, a subject and a plain-text
/// body. Written as snake_case JSON by <see cref="ResendJson"/>.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public sealed record ResendMessage(string From, IReadOnlyList<string> To, string Subject, string Text)
{
    /// <summary>
    /// The mail for a lead: one line a subject, the three fields and the time as the body; a demonstration's
    /// (F127.2) marked a demo in both.
    /// </summary>
    public static ResendMessage Of(Lead lead, LeadsOptions options)
    {
        bool demo = lead.Demonstration;
        var fields = new
        {
            lead.Identity,
            lead.Name,
            lead.Contact,
            lead.Wants,
            At = lead.At.ToString("u")
        };
        return new ResendMessage(options.From, [options.To!],
            MessageText.Format(demo ? LeadsMailText.DemoSubject : LeadsMailText.Subject, fields),
            MessageText.Format(demo ? LeadsMailText.DemoBody : LeadsMailText.Body, fields));
    }
}
