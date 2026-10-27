namespace LouisAgent.Testing;

/// <summary>
/// Neutral names for evals, for a test that stores, lists or parses an eval's name and does not need a real eval to work (ADR 0043, F18 step 7.5.4). A test of an eval set's own behaviour
/// uses that set's enum member (<c>PulsEvalName</c>), never one of these and never a string.
/// </summary>
public static class SampleEval
{
    public const string Alpha = "eval-alpha";
    public const string Beta = "eval-beta";
    public const string Gamma = "eval-gamma";

    /// <summary>A test's JSON or text with <c>&lt;EvalAlpha&gt;</c>, <c>&lt;EvalBeta&gt;</c>, <c>&lt;EvalGamma&gt;</c> where it names an eval: filled with the sample names, so the input and what the test expects are one constant.</summary>
    public static string Fill(string text) => text.Replace("<EvalAlpha>", Alpha).Replace("<EvalBeta>", Beta).Replace("<EvalGamma>", Gamma);
}
