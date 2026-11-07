namespace LouisAgent.Web.Accounts.TwoFactor;

/// <summary>
/// The shared key the way an authenticator app takes it (F61.5): as an <c>otpauth://</c> link the phone opens
/// straight into the app, and as text in groups of four for typing it in. The site's host is the issuer, so the
/// app shows where the code is for without the framework naming a site.
/// </summary>
public static class OtpAuth
{
    /// <summary>The link for the app: issuer and account in the label, the key and the issuer again as query.</summary>
    public static string Link(string issuer, string email, string key) =>
        $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(email)}" +
        $"?secret={key}&issuer={Uri.EscapeDataString(issuer)}&digits=6";

    /// <summary>The key in lowercase groups of four, easier to read off and type.</summary>
    public static string Readable(string key)
    {
        string lower = key.ToLowerInvariant();
        return string.Join(' ',
            Enumerable.Range(0, (lower.Length + 3) / 4)
                .Select(i => lower.Substring(i * 4, Math.Min(4, lower.Length - i * 4))));
    }
}
