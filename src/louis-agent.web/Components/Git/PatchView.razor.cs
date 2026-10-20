using LouisAgent.Web.Git;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Git;

public partial class PatchView
{
    [Parameter, EditorRequired] public string Diff { get; set; } = "";
    [Parameter] public bool Truncated { get; set; }

    private string? _parsed;
    private List<PatchFile> _files = [];

    protected override void OnParametersSet()
    {
        if (ReferenceEquals(_parsed, Diff)) return;
        _parsed = Diff;
        _files = Patch.Split(Diff);
    }
}
