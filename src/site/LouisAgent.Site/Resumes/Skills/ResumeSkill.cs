namespace LouisAgent.Site.Resumes.Skills;

/// <summary>
/// A group of skills on the CV (F142): its name and the skills in it. Never a rating: a self-scored 1 to 5 isn't
/// believed, so the group says what, not how well.
/// </summary>
/// <param name="Name">The group, such as AI engineering.</param>
/// <param name="Keywords">The skills in it.</param>
public sealed record ResumeSkill(string Name, IReadOnlyList<string>? Keywords = null)
{
    /// <summary>The skills, none when the file names none.</summary>
    public IReadOnlyList<string> Keywords { get; init; } = Keywords ?? [];
}
