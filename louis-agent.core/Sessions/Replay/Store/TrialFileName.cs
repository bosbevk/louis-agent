using System.Security.Cryptography;
using System.Text;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Sessions.Messages;

namespace LouisAgent.Core.Sessions.Replay.Store;

/// <summary>
/// A trial's key as a file name (ADR 0004: model- and run-supplied text never becomes a path except through this map). Every
/// character outside <c>[A-Za-z0-9_-]</c> becomes <c>_</c>, the name is cut to 100 characters, then <c>-</c> and the first 12 hex
/// digits of the key's SHA-256 are added, so two keys that clean to the same text keep apart.
/// </summary>
public static class TrialFileName
{
    public const int MaxCleanLength = 100;

    private const int HashLength = 12;

    public static string Of(string trial)
    {
        var clean = new StringBuilder(Math.Min(trial.Length, MaxCleanLength));
        foreach (char c in trial.Take(MaxCleanLength)) clean.Append(c is '_' or '-' || char.IsAsciiLetterOrDigit(c) ? c : '_');
        string hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(trial)))[..HashLength];
        return $"{clean}-{hash}";
    }

    /// <summary>The path of <paramref name="fileName"/> in <paramref name="folder"/>; throws when it would sit anywhere else.</summary>
    public static string Under(string folder, string fileName)
    {
        string root = Path.GetFullPath(folder);
        string full = Path.GetFullPath(Path.Combine(root, fileName));
        if (!full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new InvalidOperationException(MessageText.Format(SessionErrorMessage.OutsideFolder, new { Name = fileName }));
        return full;
    }
}
