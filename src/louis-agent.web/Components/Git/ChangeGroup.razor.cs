using LouisAgent.Web.Git;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Git;

public partial class ChangeGroup
{
    [Parameter, EditorRequired] public string Title { get; set; } = "";

    /// <summary>Which side the group shows: the index (staged) or the working tree.</summary>
    [Parameter] public bool Staged { get; set; }
    [Parameter, EditorRequired] public List<GitFile> Files { get; set; } = default!;

    /// <summary>The panel's selection, by <see cref="ChangesPanel.Key"/>; read here, changed through the callbacks.</summary>
    [Parameter, EditorRequired] public IReadOnlySet<string> Selected { get; set; } = default!;

    /// <summary>The key of the diff open in the viewer, to mark its row.</summary>
    [Parameter] public string? OpenKey { get; set; }

    [Parameter] public EventCallback<(GitFile File, bool Staged)> OnOpenDiff { get; set; }
    [Parameter] public EventCallback<string> OnToggle { get; set; }

    /// <summary>The select-all box: true selects every file in the group, false clears them.</summary>
    [Parameter] public EventCallback<bool> OnToggleAll { get; set; }
}
