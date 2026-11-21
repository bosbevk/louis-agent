namespace LouisAgent.Web.Components.Site.Product.Scene;

/// <summary>
/// One step of a scene in the product window: what happens in the chat, its words, how long it takes for a thought
/// or a wait, and what it changes on the page: the stage, the address, the line under the chat and the request's
/// status.
/// </summary>
public sealed record SceneStep(SceneStepKind Kind, string Text = "", int Ms = 0)
{
    /// <summary>How far the page comes with this step; none by default.</summary>
    public SceneStage Stage { get; init; }

    /// <summary>The address the window's bar shows from this step on, if it changes.</summary>
    public string? Address { get; init; }

    /// <summary>The line under the chat from this step on, such as "1 call", if it changes.</summary>
    public string? Foot { get; init; }

    /// <summary>The request's status from this step on, such as "Sent to the owner", if it changes.</summary>
    public string? Status { get; init; }
}
