namespace LouisAgent.Puls.Admin.Charts;

/// <summary>The four kinds of token a request counts, in the order the explorer draws them (F18 step 6a.3b, out of RunExplorer): their names, their colours from the palette, and the title each shows with its count.</summary>
public static class TokenKinds
{
    public static readonly IReadOnlyList<string> Names = ["input", "cache written", "cache read", "output"];

    public static IReadOnlyList<string> Colours { get; } = [.. Names.Select((_, i) => ChartPalette.Series(i))];

    /// <summary>"input 1,200" for each kind, from the four counts.</summary>
    public static IReadOnlyList<string> Titles(IReadOnlyList<long> parts) => [.. Names.Select((name, i) => $"{name} {parts[i]:N0}")];
}
