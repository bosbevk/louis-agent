namespace LouisAgent.Testing;

/// <summary>
/// Neutral names for roles, for a test that stores, shows or parses a role and does not need a real one to work (ADR 0043, F18 step 7.5.4). A test that opens a session on a real role
/// uses the identity's enum member (<c>PulsRole</c>), never one of these and never a string.
/// </summary>
public static class SampleRole
{
    public const string Alpha = "role-alpha";
    public const string Beta = "role-beta";
    public const string Gamma = "role-gamma";
    public const string Delta = "role-delta";
    public const string Epsilon = "role-epsilon";
    public const string Zeta = "sample-minion-lookup";
    public const string Eta = "role-eta";

    /// <summary>A test's JSON or text with <c>&lt;RoleAlpha&gt;</c>... where it names a role: filled with the sample names, so the input and what the test expects are one constant.</summary>
    public static string Fill(string text) => text.Replace("<RoleAlpha>", Alpha).Replace("<RoleBeta>", Beta).Replace("<RoleGamma>", Gamma).Replace("<RoleDelta>", Delta)
        .Replace("<RoleEpsilon>", Epsilon).Replace("<RoleZeta>", Zeta).Replace("<RoleEta>", Eta);
}
