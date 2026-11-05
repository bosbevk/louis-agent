using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Landing;

public partial class MiniChat
{
    /// <summary>The frame's class: <c>mini</c> on a card's stage, <c>panel</c> in the pinned tour.</summary>
    [Parameter] public string Class { get; set; } = "panel";

    /// <summary>A chip above the question for a file the user attached ("verslag.pdf · 12 bladsye").</summary>
    [Parameter] public string? Attachment { get; set; }
    [Parameter] public string? Question { get; set; }

    /// <summary>The toolset the delegation card shows; none, no card.</summary>
    [Parameter] public string? Toolset { get; set; }

    /// <summary>Still working: the card says Besig instead of Klaar.</summary>
    [Parameter] public bool Running { get; set; }

    /// <summary>The card's short head ("puls · web"), as the third step of Hoe dit werk draws it.</summary>
    [Parameter] public bool Compact { get; set; }

    /// <summary>What the specialist gave: the answer, its steps, the commits, or a cost sheet.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public string[]? Sources { get; set; }
    [Parameter] public string? Usage { get; set; }

    /// <summary>The usage as a span (a stage's line) rather than a block (a panel's).</summary>
    [Parameter] public bool UsageInline { get; set; }
}
