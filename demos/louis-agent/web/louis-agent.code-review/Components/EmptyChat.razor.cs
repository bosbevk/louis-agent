using Microsoft.AspNetCore.Components;

namespace LouisAgent.CodeReview.Components;

/// <summary>What a new chat shows: the boot screen, with the frame's message box under it.</summary>
public partial class EmptyChat
{
    /// <summary>The frame's message box.</summary>
    [Parameter, EditorRequired] public RenderFragment Composer { get; set; } = null!;
}
