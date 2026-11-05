namespace LouisAgent.Evals.Readers.Kinds;

/// <summary>
/// Reads the images a text shows, on a page the agent read or in its answer, leaving out what the reader's domain
/// knows isn't one (a page's share buttons and logos, say).
/// </summary>
public interface IImageReader : IReader
{
    /// <summary>The addresses of the images the text shows, in order; none when it shows none.</summary>
    IReadOnlyList<string> Images(string text);

    /// <summary>
    /// Whether this page's images can be told apart, so one of them is known to be the photo to show. A page may hold
    /// only other things' pictures, and then showing none of them is no fault.
    /// </summary>
    bool KnowsThePhoto(string page) => true;
}
