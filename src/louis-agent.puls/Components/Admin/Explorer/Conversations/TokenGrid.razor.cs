using LouisAgent.Puls.Admin.Explorer.Tree;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.Explorer.Conversations;

/// <summary>Shows the tokens each conversation used, the modes side by side.</summary>
public partial class TokenGrid
{
    /// <summary>The repeat's tree.</summary>
    [Parameter, EditorRequired]
    public RunTree Tree { get; set; } = RunTree.Empty;

    /// <summary>The tasks shown, as the conversations above them are filtered.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<string> Tasks { get; set; } = [];
}
