using System.Text;

namespace LouisAgent.Testing;

/// <summary>
/// The short name a project's folder stands for, as the tests read it: <c>LouisAgent.Identities.Coding</c> is
/// <c>coding</c> and <c>LouisAgent.CodeReview</c> is <c>code-review</c>, the names identities and sites go by.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0123-ask-the-type.md">ADR 0123: ask the type</seealso>
public static class ProjectNames
{
    /// <summary>How every project's name starts.</summary>
    public const string Prefix = "LouisAgent.";

    /// <summary>How an identity project's name starts.</summary>
    public const string IdentityPrefix = "LouisAgent.Identities.";

    /// <summary>
    /// The folder's name without the prefix, its words joined by dashes in small letters, the way the folders were
    /// named before every project took its namespace's name (F167).
    /// </summary>
    public static string ShortName(string folderName, string prefix)
    {
        string name = folderName[prefix.Length..];
        var shortName = new StringBuilder(name.Length + 4);
        for (int i = 0; i < name.Length; i++)
        {
            // A capital after a small letter or a digit starts a new word.
            if (i > 0 && char.IsUpper(name[i]) && char.IsLetterOrDigit(name[i - 1]) && !char.IsUpper(name[i - 1]))
                shortName.Append('-');
            shortName.Append(char.ToLowerInvariant(name[i]));
        }

        return shortName.ToString();
    }
}
