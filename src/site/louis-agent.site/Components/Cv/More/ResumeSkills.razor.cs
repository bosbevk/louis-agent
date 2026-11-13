using LouisAgent.Site.Resumes.Skills;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Site.Components.Cv.More;

/// <summary>
/// The CV's skills (F142): a panel per group, three a row, one on a phone. Grouped, never rated: a self-scored 1 to 5
/// isn't believed, so each group says what, not how well.
/// </summary>
public partial class ResumeSkills
{
    /// <summary>The groups, in the file's order.</summary>
    [Parameter]
    public IReadOnlyList<ResumeSkill> Skills { get; set; } = [];
}
