namespace LouisAgent.Puls.Admin.Explorer.Legends;

/// <summary>
/// The explorer's own colours: Puls's requests against the specialist's, and right, estimated, guessed, wrong and
/// everything else. The modes' colours are the palette's (<c>ChartPalette</c>).
/// </summary>
public static class ExplorerColours
{
    /// <summary>Puls's own requests: the palette's first slot.</summary>
    public const string Own = "#3987e5";

    /// <summary>The specialist's requests: Puls's orange.</summary>
    public const string Spec = "#f58220";

    /// <summary>Right: Puls's own green, which reads on its dark surface.</summary>
    public const string Right = "#38b449";

    /// <summary>Estimated: a lighter green, priced but not from a shop.</summary>
    public const string Estimate = "#9ad6a2";

    /// <summary>Wrong: Puls's danger red.</summary>
    public const string Wrong = "#f26b4f";

    /// <summary>Guessed: orange, a price with no source.</summary>
    public const string Guess = "#f58220";

    /// <summary>Everything else: grey.</summary>
    public const string Other = "#8a8f98";
}
