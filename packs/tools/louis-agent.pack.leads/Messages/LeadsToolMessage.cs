using System.ComponentModel;

namespace LouisAgent.Packs.Leads.Messages;

/// <summary>The leads pack's texts the model reads (ADR 0009), pinned by the tool-message golden.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
public enum LeadsToolMessage
{
    [Description("Not sent: I need the person's name first. Ask for it.")]
    NoName,

    [Description("Not sent: I need a way to reach them, a mail address or a phone number. Ask for it.")]
    NoContact,

    [Description("Not sent: I need a line on what they want, in their words. Ask for it.")]
    NoWants,

    [Description("Sent to the owner at {At}: {Name}, {Contact}, about {Wants}. Tell them they will hear back.")]
    Sent,

    [Description(
        "Not sent: the mail service answered {Reason}. Tell them to use the contact page, and try once more later.")]
    Failed,

    [Description(
        "Not mailed: this instance has no mail key, so the lead went to the log only ({Name}, {Contact}, about " +
        "{Wants}). Tell them to use the contact page.")]
    Logged,
}
