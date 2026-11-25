using LouisAgent.Composition.Packs.Files;

namespace LouisAgent.Core.Sessions.Files;

/// <summary>
/// The files a pack's tool finds through its pack context (F171.1): whichever conversation's turn is running when the
/// tool asks, as <see cref="SessionFiles" /> holds them.
/// </summary>
/// <remarks>
/// Proxy: the one pack context every session shares hands out this one object, and it asks the flow on each call, so
/// a tool built once for the whole host still reads the files of the chat it is serving.
/// </remarks>
public sealed class TurnSessionFiles : ISessionFiles
{
    /// <summary>The one there is.</summary>
    public static readonly TurnSessionFiles Instance = new();

    private TurnSessionFiles()
    {
    }

    /// <inheritdoc/>
    public SessionFile? Find(string name) => SessionFiles.Current.Find(name);

    /// <inheritdoc/>
    public void Forget(string name) => SessionFiles.Current.Forget(name);
}
