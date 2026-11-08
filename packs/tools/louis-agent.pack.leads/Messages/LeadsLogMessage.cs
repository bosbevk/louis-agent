using System.ComponentModel;

namespace LouisAgent.Packs.Leads.Messages;

/// <summary>The leads pack's log lines (ADR 0009).</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
public enum LeadsLogMessage
{
    [Description("Lead (not sent, no mail key): {Name} · {Contact} · {Wants} · {At}")]
    Logged,
}
