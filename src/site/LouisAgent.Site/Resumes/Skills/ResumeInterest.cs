namespace LouisAgent.Site.Resumes.Skills;

/// <summary>Something Louis does outside work (F142), and what about it.</summary>
/// <param name="Name">The interest, such as Chess.</param>
/// <param name="Keywords">What about it.</param>
public sealed record ResumeInterest(string Name, IReadOnlyList<string>? Keywords = null)
{
    /// <summary>What about it, none when the file says nothing.</summary>
    public IReadOnlyList<string> Keywords { get; init; } = Keywords ?? [];
}
