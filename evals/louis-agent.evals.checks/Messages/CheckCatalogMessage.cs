using System.ComponentModel;

namespace LouisAgent.Evals.Messages;

/// <summary>
/// What the check catalogue says when it turns something away, in words the person who wrote the eval file reads.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
public enum CheckCatalogMessage
{
    [Description("check type '{Type}' can't read its parameters: {Reason}")]
    BadParameters,

    [Description("check type '{Type}' was given no parameters")]
    NoParameters,
}
