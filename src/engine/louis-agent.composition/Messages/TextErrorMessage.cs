using System.ComponentModel;

namespace LouisAgent.Composition.Messages;

/// <summary>What can go wrong filling in a message's template, or reading a log setting.</summary>
public enum TextErrorMessage
{
    [Description("{Message} needs a value for '{Hole}'.")]
    MessageNeedsValue,

    [Description("{Message} has no [Description].")]
    MessageHasNoTemplate,

    [Description("Unknown LOG_CONTENT '{Value}'. Use {Supported}.")]
    UnknownLogContent,
}
