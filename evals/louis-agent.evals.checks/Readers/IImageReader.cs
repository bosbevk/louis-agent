namespace LouisAgent.Evals.Readers;

/// <summary>
/// Reads the images a text shows, on a page the agent read or in its answer, leaving out what the reader's domain
/// knows isn't one (a page's share buttons and logos, say).
/// </summary>
public interface IImageReader : IReader
{
    /// <summary>The addresses of the images the text shows, in order; none when it shows none.</summary>
    IReadOnlyList<string> Images(string text);
}
