using LouisAgent.CodeReview.Git;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.CodeReview.Components.Git;

public partial class HistoryViewer
{
    [Parameter] public GitBranchDetail? Branch { get; set; }
    [Parameter] public GitCommitDetail? Commit { get; set; }
    [Parameter] public bool Busy { get; set; }
    [Parameter] public EventCallback<string> OnMerge { get; set; }
    [Parameter] public EventCallback<string> OnDelete { get; set; }
    [Parameter] public EventCallback<string> OnOpenCommit { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
}
