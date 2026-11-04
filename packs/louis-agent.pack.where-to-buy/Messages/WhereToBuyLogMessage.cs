using System.ComponentModel;

namespace LouisAgent.Packs.WhereToBuy.Messages;

/// <summary>The where-to-buy pack's log lines (ADR 0009): what went wrong with the map, for whoever runs the host.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
public enum WhereToBuyLogMessage
{
    [Description("OpenStreetMap didn't answer a lookup for {Place} ({Reason}); the tool says so to the model.")]
    MapUnavailable,
}
