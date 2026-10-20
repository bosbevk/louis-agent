using LouisAgent.Web.Git;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Git;

public partial class DiffViewer
{
    [Parameter, EditorRequired] public GitDiff Diff { get; set; } = default!;
    [Parameter] public bool Busy { get; set; }
    [Parameter] public EventCallback OnStage { get; set; }
    [Parameter] public EventCallback OnUnstage { get; set; }
    [Parameter] public EventCallback OnDiscard { get; set; }
    [Parameter] public EventCallback OnReview { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }

    private GitDiff? _parsed;
    private List<DiffRow> _rows = [];
    private int _added, _removed;
    private bool _confirmDiscard;

    protected override void OnParametersSet()
    {
        if (ReferenceEquals(_parsed, Diff)) return;
        _parsed = Diff;
        _confirmDiscard = false;
        (_rows, _added, _removed) = UnifiedDiff.Parse(Diff.Diff);
    }

    private async Task ConfirmDiscard()
    {
        _confirmDiscard = false;
        await OnDiscard.InvokeAsync();
    }
}
