using LouisAgent.Packs.Git.Web.Branches.Records;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Packs.Git.Web.Views.Branches;

/// <summary>An open branch or commit: what's in it, and the buttons to merge or delete a branch.</summary>
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
