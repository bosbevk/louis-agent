using LouisAgent.Site.Resumes.Skills;
using LouisAgent.Web.Components.Editing;
using LouisAgent.Web.Components.Editing.Inline;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Site.Components.Cv.More;

/// <summary>
/// The CV's skills (F142): a panel per group with its skills as tags. When someone listens for a change (the
/// admin's editor), a group's name is changed where it stands, its skills are tags to add and take out, and the
/// groups are a list. It never changes the skills it was given. It hands back a new list.
/// </summary>
public partial class ResumeSkills
{
    /// <summary>A group to fill in.</summary>
    private static readonly ResumeSkill Blank = new("");

    /// <summary>The groups, in the file's order.</summary>
    [Parameter]
    public IReadOnlyList<ResumeSkill> Skills { get; set; } = [];

    /// <summary>Told the groups with one thing changed. With nothing listening, they are plain words.</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<ResumeSkill>> SkillsChanged { get; set; }

    /// <summary>Whether the skills are being edited.</summary>
    private bool Editable => SkillsChanged.HasDelegate;

    /// <summary>The listener for a group's name, which is never left out: an emptied one is blank.</summary>
    private EventCallback<string?> NameChanged(int at) => SkillsChanged.Of<IReadOnlyList<ResumeSkill>, string?>(
        this, name => Skills.Replaced(at, Skills[at] with { Name = name ?? "" }));

    /// <summary>A group's skills, with one added or taken out.</summary>
    private Task TagsAsync(int at, IReadOnlyList<string> tags) =>
        SkillsChanged.InvokeAsync(Skills.Replaced(at, Skills[at] with { Keywords = tags }));

    /// <summary>A group moves a step.</summary>
    private Task MoveAsync(int at, int step) => SkillsChanged.InvokeAsync(Skills.Moved(at, step));

    /// <summary>A group is taken out.</summary>
    private Task RemoveAsync(int at) => SkillsChanged.InvokeAsync(Skills.Without(at));

    /// <summary>One more group, blank, at the end.</summary>
    private Task AddAsync() => SkillsChanged.InvokeAsync(Skills.Added(Blank));
}
