using System.ComponentModel;

namespace LouisAgent.Packs.ShopPrices.Messages;

/// <summary>The shop-prices pack's log lines (ADR 0009): what went wrong with the price store, for whoever runs the host.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
public enum ShopPriceLogMessage
{
    [Description(
        "The price store didn't answer ({Reason}); the prices the pack ships stand in until it is read again in " +
        "{Minutes} minutes.")]
    StoreUnavailable,
}
