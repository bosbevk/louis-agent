namespace LouisAgent.Composition.Packs.Files;

/// <summary>A host that keeps no files: it finds none, and has nothing to forget (F171.1).</summary>
/// <remarks>
/// Null Object, as <see cref="Sites.NoSitePlaces" /> is: a tool asks every host the same way and reads "no file" from
/// what it finds, so a host written before files needs no change.
/// </remarks>
public sealed class NoSessionFiles : ISessionFiles
{
    /// <summary>The one there is.</summary>
    public static readonly NoSessionFiles Instance = new();

    private NoSessionFiles()
    {
    }

    /// <inheritdoc/>
    public SessionFile? Find(string name) => null;

    /// <inheritdoc/>
    public void Forget(string name)
    {
    }
}
