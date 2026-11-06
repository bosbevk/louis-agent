using LouisAgent.CodeReview.Git;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.CodeReview.Components.Git;

/// <summary>A patch across several files, as a section per file.</summary>
public partial class PatchView
{
    [Parameter, EditorRequired] public string Diff { get; set; } = "";
    [Parameter] public bool Truncated { get; set; }

    private string? _parsed;
    private List<PatchFile> _files = [];

    /// <summary>Splits the patch only when a new one comes in, not on every draw.</summary>
    protected override void OnParametersSet()
    {
        if (ReferenceEquals(_parsed, Diff)) return;
        _parsed = Diff;
        _files = Patch.Split(Diff);
    }
}
