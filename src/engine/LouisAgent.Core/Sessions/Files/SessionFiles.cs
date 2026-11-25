using LouisAgent.Composition.Packs.Files;

namespace LouisAgent.Core.Sessions.Files;

/// <summary>
/// The photos and PDFs of the conversation in this flow (F171.1): the host (the API's session) sets them for a turn,
/// and a pack's tool finds one by name through its pack context (<see cref="TurnSessionFiles" />). Held in an
/// <see cref="AsyncLocal{T}" />, as <see cref="SessionAttachments" /> holds the text ones, since the host builds one
/// pack context for all its sessions; none by default.
/// </summary>
public static class SessionFiles
{
    private static readonly AsyncLocal<ISessionFiles?> CurrentFiles = new();

    /// <summary>Makes <paramref name="files" /> this flow's files until the result is disposed.</summary>
    public static IDisposable Use(ISessionFiles files)
    {
        var previous = CurrentFiles.Value;
        CurrentFiles.Value = files;
        return new Restore(previous);
    }

    /// <summary>The files of the conversation in this flow; none when no host set them.</summary>
    internal static ISessionFiles Current => CurrentFiles.Value ?? NoSessionFiles.Instance;

    private sealed class Restore(ISessionFiles? previous) : IDisposable
    {
        public void Dispose() => CurrentFiles.Value = previous;
    }
}
