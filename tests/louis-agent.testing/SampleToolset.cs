namespace LouisAgent.Testing;

/// <summary>
/// Neutral toolset names for a test that stores, shows or parses a name and does not need a real toolset to work (ADR 0043, F18 step 7.5.3). A test that loads a real
/// toolset (a session, an engine, a catalogue) uses core's <c>CoreToolset</c> member instead, never one of these and never a string.
/// </summary>
public static class SampleToolset
{
    public const string Alpha = "alpha";
    public const string Beta = "beta";
    public const string Gamma = "gamma";
    public const string Delta = "delta";
    public const string Epsilon = "epsilon";

    /// <summary>A test's JSON or text with <c>&lt;Alpha&gt;</c>, <c>&lt;Beta&gt;</c>... where it names a toolset: filled with the sample names, so the input and what the test expects are one constant.</summary>
    public static string Fill(string text) =>
        text.Replace("<Alpha>", Alpha).Replace("<Beta>", Beta).Replace("<Gamma>", Gamma).Replace("<Delta>", Delta).Replace("<Epsilon>", Epsilon);
}
