namespace LouisAgent.Packs.Leads.Sending;

/// <summary>
/// One mail as Resend's API takes it (<c>POST /emails</c>): the sender, the recipients, a subject and a plain-text
/// body. Written as snake_case JSON by <see cref="ResendJson"/>.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public sealed record ResendMessage(string From, IReadOnlyList<string> To, string Subject, string Text)
{
    /// <summary>The mail for a lead: one line a subject, the three fields and the time as the body.</summary>
    public static ResendMessage Of(Lead lead, LeadsOptions options) => new(
        options.From,
        [options.To!],
        $"Lead from the site: {lead.Name}",
        $"Name: {lead.Name}\nReach them: {lead.Contact}\nWants: {lead.Wants}\nAt: {lead.At:u}\n\nSent by the guide on louis-agent.dev.");
}
