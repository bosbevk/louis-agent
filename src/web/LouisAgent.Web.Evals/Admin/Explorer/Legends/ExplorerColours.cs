namespace LouisAgent.Web.Evals.Admin.Explorer.Legends;

/// <summary>
/// The explorer's own colours: the agent's requests against the specialist's, and right, wrong and everything else.
/// The modes' colours are the palette's (<c>ChartPalette</c>).
/// </summary>
public static class ExplorerColours
{
    /// <summary>The agent's own requests: the palette's first slot.</summary>
    public const string Own = "#3987e5";

    /// <summary>The specialist's requests: an orange.</summary>
    public const string Spec = "#f58220";

    /// <summary>Right: a green that reads on a dark surface.</summary>
    public const string Right = "#38b449";

    /// <summary>Wrong: a danger red.</summary>
    public const string Wrong = "#f26b4f";

    /// <summary>Everything else: grey.</summary>
    public const string Other = "#8a8f98";
}
