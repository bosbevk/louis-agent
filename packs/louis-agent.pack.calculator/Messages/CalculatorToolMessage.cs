using System.ComponentModel;

namespace LouisAgent.Packs.Calculator.Messages;

/// <summary>
/// The calculator's texts the model reads (ADR 0009), pinned by <c>CalculatorToolMessageSnapshot.json</c>.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
public enum CalculatorToolMessage
{
    [Description("Calculate needs at least one sum, such as '500*3/2'.")]
    NoSums,

    [Description("At most {Most} sums in one call; this one had {Count}. Split them into two calls.")]
    TooManySums,

    [Description("{Sum} = {Result}")]
    SumWorkedOut,

    [Description(
        "{Sum}: can't be worked out. Use only numbers, + - * / and brackets, with a point for decimals and a division " +
        "for a fraction (2/3).")]
    SumRefused,
}
