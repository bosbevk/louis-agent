namespace LouisAgent.Puls.Admin.Explorer.Legends;

/// <summary>
/// The explorer's own colours: Puls's requests against the specialist's, and right, wrong and everything else. The
/// modes' colours are the palette's (<c>ChartPalette</c>).
/// </summary>
public static class ExplorerColours
{
    /// <summary>Puls's own requests: the palette's first slot.</summary>
    public const string Own = "#3987e5";

    /// <summary>The specialist's requests: Puls's orange.</summary>
    public const string Spec = "#f58220";

    /// <summary>Right: Puls's own green, which reads on its dark surface.</summary>
    public const string Right = "#38b449";

    /// <summary>Wrong: Puls's danger red.</summary>
    public const string Wrong = "#f26b4f";

    /// <summary>Everything else: grey.</summary>
    public const string Other = "#8a8f98";
}
