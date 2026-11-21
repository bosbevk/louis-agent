namespace LouisAgent.Web.Components.Site.Product.Scene;

/// <summary>What one step of a scene does in the window's chat.</summary>
public enum SceneStepKind
{
    /// <summary>The visitor asks, typed out.</summary>
    Ask,

    /// <summary>The agent thinks for a moment, then the line goes.</summary>
    Think,

    /// <summary>The agent calls a tool: it spins while it runs, then stands as done.</summary>
    Call,

    /// <summary>The agent answers, typed out.</summary>
    Answer,

    /// <summary>The agent offers its choices, the words split by a bar.</summary>
    Offer,

    /// <summary>The visitor presses the first choice offered.</summary>
    Press,

    /// <summary>Nothing happens for a while.</summary>
    Wait
}
