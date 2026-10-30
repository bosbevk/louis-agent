namespace LouisAgent.Identities.Puls.Tests.Evals.Checks;

/// <summary>
/// The cook scenarios' ids these tests name more than once. An id is data in the dataset file, not an enum, so it is
/// one constant here; <c>PulsIdsTests</c> holds each to the dataset, so a typo fails there instead of quietly finding
/// no scenario.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0043-repeated-names-are-enums.md">ADR 0043: repeated names are enums</seealso>
public static class PulsCookScenarioIds
{
    /// <summary>The cook scenario asked in Afrikaans.</summary>
    public const string CookAf = "cook-af";

    /// <summary>The cook scenario asked in English.</summary>
    public const string CookEn = "cook-en";

    /// <summary>
    /// Fills <c>&lt;CookAf&gt;</c> and <c>&lt;CookEn&gt;</c> in a test's JSON or text with the ids, so the input and
    /// what the test expects use the same constant.
    /// </summary>
    public static string Fill(string text) => text.Replace("<CookAf>", CookAf).Replace("<CookEn>", CookEn);
}
