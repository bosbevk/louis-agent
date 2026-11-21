using System.ComponentModel;

namespace LouisAgent.Packs.Leads.Messages;

/// <summary>
/// The lead's mail to the owner (ADR 0009): its subject and its body, a real lead's and a demonstration's (F127.2,
/// Louis's pick "mails you, marked demo"), so a try-out on a showroom never reads as a real customer's.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
public enum LeadsMailText
{
    [Description("Lead from the site: {Name}")]
    Subject,

    [Description(
        "Name: {Name}\nReach them: {Contact}\nWants: {Wants}\nAt: {At}\n\nSent by the guide on louis-agent.dev.")]
    Body,

    [Description("[demo] {Identity}: {Name}")]
    DemoSubject,

    [Description(
        "Name: {Name}\nReach them: {Contact}\nWants: {Wants}\nAt: {At}\n\nSent from a showroom demo on " +
        "louis-agent.dev: someone tried {Identity}'s agent, a demonstration business.")]
    DemoBody,
}
