namespace LouisAgent.Core.Sessions;

/// <summary>
/// The files the user attached in this conversation, by name (F22): the host (the API's session) sets them for a turn,
/// and <see cref="SpecialistTools.StartSubSession"/> forwards the ones the model names to the specialist. The model passes
/// names, not contents, so a large file isn't copied through the model's output. Held in an <see cref="AsyncLocal{T}"/>,
/// like <see cref="SubSessionFeed"/>; empty by default.
/// </summary>
public static class SessionAttachments
{
    private static readonly IReadOnlyDictionary<string, string> None = new Dictionary<string, string>();
    private static readonly AsyncLocal<IReadOnlyDictionary<string, string>?> CurrentAttachments = new();

    /// <summary>Makes <paramref name="attachments"/> (name → content) this flow's attachments until the result is disposed.</summary>
    public static IDisposable Use(IReadOnlyDictionary<string, string> attachments)
    {
        var previous = CurrentAttachments.Value;
        CurrentAttachments.Value = attachments;
        return new Restore(previous);
    }

    /// <summary>The attachments of the conversation in this flow; none when no host set them.</summary>
    internal static IReadOnlyDictionary<string, string> Current => CurrentAttachments.Value ?? None;

    private sealed class Restore(IReadOnlyDictionary<string, string>? previous) : IDisposable
    {
        public void Dispose() => CurrentAttachments.Value = previous;
    }
}
