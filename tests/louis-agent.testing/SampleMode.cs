namespace LouisAgent.Testing;

/// <summary>
/// Neutral names for the modes of an eval run, for a test that stores, shows or parses a mode and does not need a real eval's mode to work (ADR 0043, F18 step 7.5.4).
/// A test of an eval set's own behaviour uses that set's enum member (<c>CookMode</c>), never one of these and never a string.
/// </summary>
public static class SampleMode
{
    public const string Alpha = "mode-alpha";
    public const string Beta = "mode-beta";
    public const string Gamma = "mode-gamma";

    /// <summary>A test's JSON or text with <c>&lt;ModeAlpha&gt;</c>, <c>&lt;ModeBeta&gt;</c>, <c>&lt;ModeGamma&gt;</c> where it names a mode: filled with the sample names, so the input and what the test expects are one constant.</summary>
    public static string Fill(string text) => text.Replace("<ModeAlpha>", Alpha).Replace("<ModeBeta>", Beta).Replace("<ModeGamma>", Gamma);
}
