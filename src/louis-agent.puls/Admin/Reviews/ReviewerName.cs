namespace LouisAgent.Puls.Admin.Reviews;

/// <summary>
/// The name a reviewer gives (F18 step 6a.3). The portal's gate is one shared key, so there is no logged-in person: a grade carries a name the person typed, which says who said they graded,
/// not who proved it. The portal checks what the API checks (not blank, at most <see cref="MaxLength"/> characters), so it never sends a grade the API would refuse.
/// </summary>
public static class ReviewerName
{
    public const int MaxLength = 100;

    /// <summary>The name trimmed, or null when it is blank or too long to be kept.</summary>
    public static string? Clean(string? typed)
    {
        string name = typed?.Trim() ?? "";
        return name.Length is > 0 and <= MaxLength ? name : null;
    }
}
