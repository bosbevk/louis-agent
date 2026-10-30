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

    [Description("check type '{Type}' needs at least one item in '{Field}'")]
    NothingListed,

    [Description("check type '{Type}' needs a text in '{Field}'")]
    NoText,

    [Description("check type '{Type}' needs '{Field}' to be at least 1")]
    BelowOne,

    [Description("reader '{Name}' is not in the catalogue; the readers are {Supported}")]
    UnknownReader,

    [Description("reader '{Name}' is not a {Kind}")]
    WrongReaderKind,

    [Description("reader '{Name}' is listed twice")]
    DuplicateReader,

    [Description("check type '{Type}' isn't a safe name: use letters, digits, '-' and '_', at most 64")]
    TypeNotSafe,

    [Description("check type '{Type}' is listed twice")]
    DuplicateType,

    [Description("check type '{Type}' is not in the catalogue; the types are {Supported}")]
    UnknownType,
}
