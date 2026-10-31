using System.ComponentModel;

namespace LouisAgent.Packs.Shops.Messages;

/// <summary>
/// Why a shop rule passed or failed a conversation, in words a person reads in the portal: the pack's own checks'
/// reasons (F49.3), as its tools' texts are <see cref="ShopToolMessage"/>.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
public enum ShopCheckMessage
{
    [Description("the list has a column for a shop the cook can't read: {Shops}")]
    UntooledShopColumn,

    [Description("every shop the list has a column for is one the cook can read")]
    UntooledShopsNone,
}
