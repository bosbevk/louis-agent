namespace LouisAgent.Core.Hosting.Engines;

using System.Collections.Concurrent;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Hosting.Messages;
using LouisAgent.Core.Logging;
using Microsoft.Extensions.Logging;

/// <summary>
/// The factory's engines, one per <see cref="ProfileKey"/> (F5): built once on first use, outside the lock, and past <c>capacity</c> the
/// least recently used is dropped (a session keeps its own engine, so a drop never breaks a chat; the next session with that key builds it again).
/// </summary>
public sealed class EngineCache(int capacity)
{
    private static readonly ILogger Log = AgentLogging.For<EngineCache>();

    private readonly ConcurrentDictionary<ProfileKey, Lazy<AgentEngine>> _engines = new();
    private readonly ConcurrentDictionary<ProfileKey, long> _lastUsed = new();
    private readonly Lock _evicting = new();
    private long _ticks;

    /// <summary>The keys' <see cref="ProfileKey.Text"/>, for the factory's <c>Profiles</c>.</summary>
    public IReadOnlyCollection<string> Keys => _engines.Keys.Select(k => k.Text).ToList();

    public AgentEngine GetOrAdd(ProfileKey key, Func<AgentEngine> build)
    {
        _lastUsed[key] = Interlocked.Increment(ref _ticks);
        if (_engines.TryGetValue(key, out var existing)) return existing.Value;

        Lazy<AgentEngine> engine;
        lock (_evicting)
        {
            while (_engines.Count >= capacity && !_engines.ContainsKey(key))
            {
                var oldest = _engines.Keys.Where(k => k != key).MinBy(k => _lastUsed.GetValueOrDefault(k))!;
                _engines.TryRemove(oldest, out _);
                _lastUsed.TryRemove(oldest, out _);
                Log.Log(LogLevel.Information, HostingLogMessage.ToolsetProfileDropped, new { Toolsets = oldest.Text.Length == 0 ? "(none)" : oldest.Text, Capacity = capacity });
            }
            engine = _engines.GetOrAdd(key, _ => new Lazy<AgentEngine>(build));
        }
        return engine.Value;   // built outside the lock: building one profile doesn't hold up another
    }
}
