namespace LouisAgent.Core.Tools.Shops;

using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Tools.Web.Browsing;
using Microsoft.Extensions.Logging;

/// <summary>
/// The store a shop answers a town from (F34), as the session its searches start with: one lookup per town, however many
/// searches ask at once (the cook's run in parallel), shared by every engine in the process, and kept a day in a file
/// (<c>SHOP_STORE_CACHE</c>) so a restarted container doesn't look the town up again (Louis's melktert chat waited
/// 12.5 s for Malmesbury's). A town with no store is kept too; a lookup that failed isn't, so the next search tries again.
/// Without a file, towns are kept in memory for the process's life. An unreadable file is no cache, never a failed search.
/// </summary>
public sealed class StoreSessionCache(string? file = null, Func<DateTimeOffset>? now = null)
{
    /// <summary>How long a town's store is kept: a shop's stores for a town rarely change within a day.</summary>
    public static readonly TimeSpan Keep = TimeSpan.FromDays(1);

    private static readonly ILogger Log = AgentLogging.For<StoreSessionCache>();
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    private readonly Func<DateTimeOffset> _now = now ?? (() => DateTimeOffset.UtcNow);
    private readonly ConcurrentDictionary<string, Lazy<Task<BrowserSession?>>> _lookups = new(StringComparer.Ordinal);
    private readonly object _gate = new();
    private Dictionary<string, Entry>? _kept;

    /// <summary>
    /// The town's store at the shop: kept and fresh, else looked up by <paramref name="lookup"/> once for every caller
    /// waiting (its own cancellation is the caller's wait, never the shared lookup).
    /// </summary>
    public async Task<BrowserSession?> GetOrLookUpAsync(string shop, string town, Func<Task<(BrowserSession? Session, bool Keep)>> lookup,
        CancellationToken cancellationToken = default)
    {
        string key = $"{shop.Trim().ToLowerInvariant()}|{town.Trim().ToLowerInvariant()}";
        if (Fresh(key) is { } kept) return kept.Session;

        var shared = _lookups.GetOrAdd(key, _ => new Lazy<Task<BrowserSession?>>(() => LookUpAsync(key, lookup)));
        return await shared.Value.WaitAsync(cancellationToken);
    }

    private async Task<BrowserSession?> LookUpAsync(string key, Func<Task<(BrowserSession? Session, bool Keep)>> lookup)
    {
        try
        {
            var (session, keep) = await lookup();
            if (keep) Save(key, new Entry(_now(), session?.Cookies.ToList()) { Session = session });
            return session;
        }
        finally
        {
            _lookups.TryRemove(key, out _);
        }
    }

    private Entry? Fresh(string key)
    {
        lock (_gate)
        {
            _kept ??= Read();
            return _kept.TryGetValue(key, out var entry) && _now() - entry.At < Keep ? entry : null;
        }
    }

    private void Save(string key, Entry entry)
    {
        lock (_gate)
        {
            // Another process (Puls and its specialist share the file) may have kept other towns since: merge, then write.
            var kept = Read();
            foreach (var (k, v) in _kept ?? []) kept.TryAdd(k, v);
            kept[key] = entry;
            _kept = kept;
            if (file is null) return;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(file))!);
                string temp = file + ".tmp";
                File.WriteAllText(temp, JsonSerializer.Serialize(kept, Json));
                File.Move(temp, file, overwrite: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Log.Log(LogLevel.Warning, LogMessage.ShopStoreCacheUnwritable, new { Path = file, Reason = ex.Message });
            }
        }
    }

    private Dictionary<string, Entry> Read()
    {
        if (file is null || !File.Exists(file)) return new(_kept ?? [], StringComparer.Ordinal);
        try
        {
            var read = JsonSerializer.Deserialize<Dictionary<string, Entry>>(File.ReadAllText(file), Json) ?? [];
            return new(read, StringComparer.Ordinal);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            Log.Log(LogLevel.Warning, LogMessage.ShopStoreCacheUnreadable, new { Path = file, Reason = ex.Message });
            return new(_kept ?? [], StringComparer.Ordinal);
        }
    }

    /// <summary>A town's store when it was looked up: the shop's cookies, or null for no store there.</summary>
    private sealed record Entry(DateTimeOffset At, List<BrowserCookie>? Cookies)
    {
        private BrowserSession? _session;

        /// <summary>The session, built once: every search from the town starts with the same one.</summary>
        [JsonIgnore]
        public BrowserSession? Session
        {
            get => _session ??= Cookies is null ? null : new BrowserSession(Cookies);
            init => _session = value;
        }
    }
}
