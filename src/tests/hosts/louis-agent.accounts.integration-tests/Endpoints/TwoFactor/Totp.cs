using System.Globalization;
using System.Net;
using System.Security.Cryptography;

namespace LouisAgent.Accounts.IntegrationTests.Endpoints.TwoFactor;

/// <summary>
/// What an authenticator app does with the shared key: the six-digit code for the current half minute (RFC 6238,
/// HMAC-SHA1, as Identity's authenticator token provider checks it), so a test can turn two-factor on and sign in
/// with it without a phone.
/// </summary>
internal static class Totp
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    /// <summary>The code the app would show right now for this key.</summary>
    public static string Now(string base32Key)
    {
        long step = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;
        byte[] counter = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(step));
        byte[] hash = HMACSHA1.HashData(FromBase32(base32Key), counter);

        // Dynamic truncation: four bytes from where the last nibble points, the sign bit dropped, six digits kept.
        int offset = hash[^1] & 0xf;
        int code = ((hash[offset] & 0x7f) << 24) | (hash[offset + 1] << 16) | (hash[offset + 2] << 8) |
                   hash[offset + 3];
        return (code % 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
    }

    /// <summary>Identity writes the key in base32 without padding; this reads it back to bytes.</summary>
    private static byte[] FromBase32(string text)
    {
        var bytes = new List<byte>(text.Length * 5 / 8);
        int buffer = 0, bits = 0;
        foreach (char c in text.TrimEnd('=').ToUpperInvariant())
        {
            buffer = (buffer << 5) | Alphabet.IndexOf(c, StringComparison.Ordinal);
            bits += 5;
            if (bits < 8) continue;
            bits -= 8;
            bytes.Add((byte)(buffer >> bits & 0xff));
        }

        return [.. bytes];
    }
}
