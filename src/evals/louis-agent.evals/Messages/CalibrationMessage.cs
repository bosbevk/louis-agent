using System.ComponentModel;

namespace LouisAgent.Evals.Messages;

/// <summary>The words of a calibration report (ADR 0009: a text is a message enum member): how often a model grader agreed with a person, in each class of label, and where it did not. Generic: nothing names what is judged.</summary>
public enum CalibrationMessage
{
    [Description("Agreement: {Agreed} of {Total}")]
    Agreement,

    [Description(
        "Labelled {Label}: judge pass {Passed}, fail {Failed}, can't tell {CantTell} ({Agreed} of {Total}, {Percent}%){Few}")]
    ClassLine,

    [Description("Labelled {Label}: no labels")]
    ClassNone,

    [Description(", few labels")] FewLabels,

    [Description("Result: pass, at least {Bar}% in each class")]
    ResultPass,

    [Description("Result: not enough: {Short}")]
    ResultNotEnough,

    [Description("labelled {Label} agreed {Agreed} of {Total} (needs {Bar}%)")]
    ShortOf,

    [Description("labelled {Label} has no labels, so it is untested")]
    ShortNone,

    [Description("{Scenario} (run {Run}, repeat {Repeat}, {Mode}): label: {Label}, judge: {Judge}: {Reason}")]
    Disagreement,

    [Description("pass")] Pass,

    [Description("fail")] Fail,

    [Description("can't tell")] CantTell,
}
