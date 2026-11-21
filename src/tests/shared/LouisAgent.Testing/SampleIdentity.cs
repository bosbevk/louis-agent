namespace LouisAgent.Testing;

/// <summary>
/// Neutral names for identities (and the agent peers that carry them), for a test that stores, shows or parses an identity's name and does not need a real one to work (ADR 0043, F18 step 7.5.4d).
/// A test that needs the real identity uses its owner's enum member (<c>PulsIdentityName.Puls</c>, <c>IdentityKind.Puls</c>), never one of these and never a string.
/// </summary>
public static class SampleIdentity
{
    public const string Alpha = "identity-alpha";
    public const string Beta = "identity-beta";

    /// <summary>A test's JSON or text with <c>&lt;IdentityAlpha&gt;</c>... where it names an identity: filled with the sample names, so the input and what the test expects are one constant.</summary>
    public static string Fill(string text) => text.Replace("<IdentityAlpha>", Alpha).Replace("<IdentityBeta>", Beta);
}
