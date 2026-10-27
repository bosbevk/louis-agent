
using System.ComponentModel;

namespace LouisAgent.Core.Config.Messages;
// The message enums (docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by MessageText.Format (or by the structured logger). Members are added
// as code is written or moved; the refactor after F4 moves the existing messages here.

/// <summary>Exception texts for the settings area: what is wrong and how to fix it.</summary>
public enum ConfigErrorMessage
{
    [Description("Unknown LLM_CACHE '{Value}'. Use on or off.")]
    UnknownCacheSetting,

    [Description("Unknown LLM_CACHE_TTL '{Value}'. Use 5m or 1h.")]
    UnknownCacheTtl,

    [Description("Unknown LLM_THINKING '{Value}'. Use off, low, medium or high.")]
    UnknownThinking,

    [Description("'{Key}' is not a setting name.")]
    NotASettingName,

    [Description("A setting's value is one line.")]
    SettingValueNotOneLine,

    [Description(
        "HOST_TIME_ZONE '{Value}' isn't a time zone this machine knows: use an IANA name such as Europe/London, or leave it unset for the machine's own.")]
    UnknownHostTimeZone,
}
