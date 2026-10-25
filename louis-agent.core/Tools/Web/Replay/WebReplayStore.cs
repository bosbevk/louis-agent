using System.Security.Cryptography;
using System.Text.Json;

namespace LouisAgent.Core.Tools.Web.Replay;

/// <summary>
/// The saved web input of eval runs (F18 step 4a): each answer once per hash in <c>{directory}/pages/{sha256}.json</c> (the JSON bytes
/// of a <see cref="ReplayPage"/>, hashed as they are), and each trial's index in <c>{directory}/trials/{safe name}.jsonl</c>, one
/// <see cref="ReplayIndexLine"/> per visit, appended under one lock because a parent and its sub-session write at once. Private: only
/// the browser opens it, nothing serves it, and <c>/data/</c> is git-ignored. A replay keeps no counters: an ask is answered from its
/// first recording.
/// </summary>
public sealed class WebReplayStore(string directory, TimeProvider? clock = null)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    private readonly SemaphoreSlim _indexLock = new(1, 1);
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;

    private string Pages => Path.Combine(directory, "pages");

    private string Trials => Path.Combine(directory, "trials");

    public async Task SavePageAsync(string trial, ReplayAsk ask, ReplayPage page, CancellationToken cancellationToken = default)
    {
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(page, Json);
        string hash = Convert.ToHexStringLower(SHA256.HashData(bytes));
        await WritePageOnceAsync(hash, bytes, cancellationToken);
        await AppendAsync(new ReplayIndexLine(trial, ask.Url, ask.Script, ask.ReadySelector, _clock.GetUtcNow(), hash, null), cancellationToken);
    }

    public Task SaveFailureAsync(string trial, ReplayAsk ask, string error, CancellationToken cancellationToken = default) =>
        AppendAsync(new ReplayIndexLine(trial, ask.Url, ask.Script, ask.ReadySelector, _clock.GetUtcNow(), null, error), cancellationToken);

    /// <summary>The first recording of <paramref name="ask"/> in the trial, or null when there is none (or no such trial).</summary>
    public async Task<(ReplayPage? Page, string? Error)?> FindAsync(string trial, ReplayAsk ask, CancellationToken cancellationToken = default)
    {
        string index = TrialFileName.Under(Trials, TrialFileName.Of(trial) + ".jsonl");
        if (!File.Exists(index)) return null;

        foreach (string text in await File.ReadAllLinesAsync(index, cancellationToken))
        {
            if (ParseLine(text) is not { } line || line.Ask != ask) continue;
            if (line.Error is not null) return (null, line.Error);
            if (await ReadPageAsync(line.Hash, cancellationToken) is { } page) return (page, null);
        }
        return null;
    }

    private static ReplayIndexLine? ParseLine(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        try { return JsonSerializer.Deserialize<ReplayIndexLine>(text, Json); }
        catch (JsonException) { return null; }
    }

    /// <summary>The page a hash names; the hash comes from a file, so it must be 64 lowercase hex digits before it is part of a path (ADR 0004).</summary>
    private async Task<ReplayPage?> ReadPageAsync(string? hash, CancellationToken cancellationToken)
    {
        if (hash is not { Length: 64 } || !hash.All(char.IsAsciiHexDigitLower)) return null;
        string path = Path.Combine(Pages, hash + ".json");
        if (!File.Exists(path)) return null;
        return JsonSerializer.Deserialize<ReplayPage>(await File.ReadAllBytesAsync(path, cancellationToken), Json);
    }

    /// <summary>Written to a temporary name and moved into place; a hash that is already there is skipped, and a racing writer is no error.</summary>
    private async Task WritePageOnceAsync(string hash, byte[] bytes, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Pages);
        string path = Path.Combine(Pages, hash + ".json");
        if (File.Exists(path)) return;

        string temporary = $"{path}.{Guid.NewGuid():N}.tmp";
        await File.WriteAllBytesAsync(temporary, bytes, cancellationToken);
        try { File.Move(temporary, path); }
        catch (IOException) when (File.Exists(path)) { File.Delete(temporary); }
    }

    private async Task AppendAsync(ReplayIndexLine line, CancellationToken cancellationToken)
    {
        string path = TrialFileName.Under(Trials, TrialFileName.Of(line.Trial) + ".jsonl");
        await _indexLock.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(Trials);
            await File.AppendAllTextAsync(path, JsonSerializer.Serialize(line, Json) + "\n", cancellationToken);
        }
        finally
        {
            _indexLock.Release();
        }
    }
}
