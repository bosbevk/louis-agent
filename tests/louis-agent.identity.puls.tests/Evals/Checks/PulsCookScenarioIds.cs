namespace LouisAgent.Identities.Puls.Tests.Evals.Checks;

/// <summary>
/// The ids of the cook dataset's scenarios that these tests name more than once (ADR 0043, F18 step 7.5.4f). A scenario id is data in the dataset file, which keeps the text, so it is not an enum: it is one
/// constant per test project where it repeats. <c>PulsIdsTests</c> holds each to the dataset, so a misspelt constant fails there and not as a scenario that quietly isn't found.
/// </summary>
public static class PulsCookScenarioIds
{
    public const string CookAf = "cook-af";
    public const string CookEn = "cook-en";

    /// <summary>A test's JSON or text with <c>&lt;CookAf&gt;</c> or <c>&lt;CookEn&gt;</c> where it names a scenario: filled with the ids, so the input and what the test expects are one constant.</summary>
    public static string Fill(string text) => text.Replace("<CookAf>", CookAf).Replace("<CookEn>", CookEn);
}
