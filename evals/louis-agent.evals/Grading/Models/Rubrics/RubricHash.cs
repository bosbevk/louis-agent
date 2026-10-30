using System.Security.Cryptography;
using System.Text;

namespace LouisAgent.Evals.Grading.Models.Rubrics;

/// <summary>
/// The hash a judge's grades carry for its rubric: the first 12 hex digits of the SHA-256 of the rubric's text with
/// its line endings made LF, so the same rubric is one hash on a Windows checkout (CRLF) and on Linux (CI, the
/// containers), and a grade made with another rubric is another measure.
/// </summary>
/// <remarks>
/// One place for the rule, which the general <c>judge</c> and an identity's own judge share (Don't Repeat Yourself).
/// Twelve digits: as long as a prompt hash is anywhere else in the ledger and the portal. Line endings are the one
/// thing normalised, since git changes them per platform and a rubric's words don't.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0014-test-first.md">ADR 0014: tests are platform-neutral</seealso>
public static class RubricHash
{
    /// <summary>The rubric's hash, the same on every platform.</summary>
    public static string Of(string rubric) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(rubric.ReplaceLineEndings("\n"))))[..12];
}
