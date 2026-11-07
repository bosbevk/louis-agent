using LouisAgent.Packs.Git.Web.Changes;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Packs.Git.Web.Views.Changes;

/// <summary>One file's diff with its line numbers, and the buttons to stage, discard or review it.</summary>
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

    /// <summary>Parses the diff only when a new one comes in, not on every draw.</summary>
    protected override void OnParametersSet()
    {
        if (ReferenceEquals(_parsed, Diff)) return;
        _parsed = Diff;
        _confirmDiscard = false;
        (_rows, _added, _removed) = UnifiedDiff.Parse(Diff.Diff);
    }

    /// <summary>The discard's second click: closes the question and tells the page.</summary>
    private async Task ConfirmDiscard()
    {
        _confirmDiscard = false;
        await OnDiscard.InvokeAsync();
    }
}
