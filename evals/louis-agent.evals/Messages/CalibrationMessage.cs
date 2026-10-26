using System.ComponentModel;

namespace LouisAgent.Evals.Messages;

/// <summary>The words of a calibration report (ADR 0009: a text is a message enum member): how often a model grader agreed with a person, and where it did not.</summary>
public enum CalibrationMessage
{
    [Description("Agreement: {Agreed} of {Total}")]
    Agreement,

    [Description("{Scenario} (run {Run}, repeat {Repeat}, {Mode}): label: {Label}, judge: {Judge}: {Reason}")]
    Disagreement,

    [Description("a meal")]
    IsAMeal,

    [Description("not a meal")]
    IsNotAMeal,

    [Description("can't tell")]
    CantTell,
}
