namespace LouisAgent.Testing;

/// <summary>
/// Neutral ids for scenarios, for a test that stores, shows or groups a scenario and does not need a real one to work (ADR 0043, F18 step 7.5.4f). A scenario id is data in a dataset file, so it
/// is not an enum; a test of an eval set itself uses that project's own ids class (<c>PulsCookScenarioIds</c>), held to its dataset.
/// </summary>
public static class SampleScenario
{
    public const string Alpha = "scenario-alpha";
    public const string Beta = "scenario-beta";

    /// <summary>A test's JSON or text with <c>&lt;ScenarioAlpha&gt;</c> or <c>&lt;ScenarioBeta&gt;</c> where it names a scenario: filled with the sample ids, so the input and what the test expects are one constant.</summary>
    public static string Fill(string text) => text.Replace("<ScenarioAlpha>", Alpha).Replace("<ScenarioBeta>", Beta);
}
