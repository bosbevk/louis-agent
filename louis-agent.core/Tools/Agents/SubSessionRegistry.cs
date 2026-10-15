namespace LouisAgent.Core.Tools.Agents;

/// <summary>
/// A chat's open sub-sessions, by handle (F26): the host (the API's session) owns one and makes it current for each
/// turn, like <see cref="SessionAttachments"/>; <see cref="AgentPeerTools.StartSubSession"/> then keeps a specialist
/// open instead of deleting it, and <see cref="AgentPeerTools.ContinueSubSession"/> finds it again. With none current
/// (an MCP call, the CLI) nothing is kept.
/// </summary>
/// <remarks>
/// The limits are in code: at most <see cref="DefaultCap"/> open per chat (the oldest is closed when another opens),
/// and <see cref="CloseAllAsync"/> when the chat ends, so a peer never keeps a sub-session nobody will use; the peer's
/// own idle timeout is the fallback. Handles count up for the chat's life and are never reused.
/// </remarks>
public sealed class SubSessionRegistry(int cap = SubSessionRegistry.DefaultCap)
{
    /// <summary>Open sub-sessions one chat may keep.</summary>
    public const int DefaultCap = 3;

    private static readonly AsyncLocal<SubSessionRegistry?> CurrentRegistry = new();

    private readonly Lock _lock = new();
    private readonly List<OpenSubSession> _open = [];
    private int _next;

    /// <summary>Makes <paramref name="registry"/> this flow's registry until the result is disposed.</summary>
    public static IDisposable Use(SubSessionRegistry registry)
    {
        var previous = CurrentRegistry.Value;
        CurrentRegistry.Value = registry;
        return new Restore(previous);
    }

    /// <summary>The chat's registry in this flow; null when no host set one.</summary>
    public static SubSessionRegistry? Current => CurrentRegistry.Value;

    /// <summary>The open sub-sessions, oldest first.</summary>
    public IReadOnlyList<OpenSubSession> Open
    {
        get { lock (_lock) return _open.ToArray(); }
    }

    /// <summary>Keeps <paramref name="entry"/> under the next handle, closing the oldest past the cap; returns the handle.</summary>
    public async Task<string> AddAsync(OpenSubSession entry)
    {
        List<OpenSubSession> evicted = [];
        string handle;
        lock (_lock)
        {
            handle = $"sub-{++_next}";
            _open.Add(entry with { Handle = handle });
            while (_open.Count > cap)
            {
                evicted.Add(_open[0]);
                _open.RemoveAt(0);
            }
        }
        foreach (var old in evicted) await old.Close();
        return handle;
    }

    /// <summary>The open sub-session with this handle, or null.</summary>
    public OpenSubSession? Find(string handle)
    {
        lock (_lock) return _open.FirstOrDefault(o => string.Equals(o.Handle, handle.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Forgets a sub-session without closing it (the peer already dropped it).</summary>
    public bool Remove(string handle)
    {
        lock (_lock) return _open.RemoveAll(o => string.Equals(o.Handle, handle.Trim(), StringComparison.OrdinalIgnoreCase)) > 0;
    }

    /// <summary>Closes every open sub-session on its peer and empties the registry: the chat is over.</summary>
    public async Task CloseAllAsync()
    {
        OpenSubSession[] all;
        lock (_lock)
        {
            all = _open.ToArray();
            _open.Clear();
        }
        foreach (var open in all) await open.Close();
    }

    private sealed class Restore(SubSessionRegistry? previous) : IDisposable
    {
        public void Dispose() => CurrentRegistry.Value = previous;
    }
}
