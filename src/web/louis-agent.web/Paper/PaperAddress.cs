namespace LouisAgent.Web.Paper;

/// <summary>
/// An address as it is written on paper (FX154), beside the QR code that holds it in full: short enough to read and,
/// if it must be, to type.
/// </summary>
public static class PaperAddress
{
    /// <summary>What an address starts with that a person leaves out when writing it down.</summary>
    private static readonly string[] LeftOut = ["https://", "http://", "mailto:", "www."];

    /// <summary>The address without its scheme, its www, a mail link's prefix or a slash at its end.</summary>
    public static string Short(string address)
    {
        string written = address.Trim();
        foreach (string start in LeftOut)
        {
            if (written.StartsWith(start, StringComparison.OrdinalIgnoreCase)) written = written[start.Length..];
        }

        return written.TrimEnd('/');
    }
}
