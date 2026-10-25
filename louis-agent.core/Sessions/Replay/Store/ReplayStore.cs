using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using LouisAgent.Core.Config;

namespace LouisAgent.Core.Sessions.Replay.Store;

/// <summary>
/// The saved web input of eval runs (F18 steps 4a and 4b): every kind of answer (<see cref="ReplayKind"/>) once per hash in
/// <c>{directory}/answers/{sha256}.json</c> (the JSON bytes of the answer, hashed as they are), and each trial's index in
/// <c>{directory}/trials/{safe name}.jsonl</c>, one <see cref="ReplayIndexLine"/> per ask, appended under one lock because a parent and its
/// sub-session write at once. An ask is a kind plus a key and an answer is any JSON value, so a new seam needs no new store. Private: only
/// the seams open it, nothing serves it, and <c>/data/</c> is git-ignored. A replay keeps no counters: an ask is answered from its
/// first recording.
/// </summary>
public sealed class ReplayStore(string directory, TimeProvider? clock = null)
{
    /// <summary>snake_case properties; enums as snake_case names, never numbers (ADR 0024).</summary>
    internal static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower, allowIntegerValues: false) },
    };

    private readonly SemaphoreSlim _indexLock = new(1, 1);
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;

    private string Answers => Path.Combine(directory, "answers");

    private string Trials => Path.Combine(directory, "trials");

    /// <summary>The process's one store: where <c>REPLAY_DIRECTORY</c> is set, else null. The only place outside the setting that names it.</summary>
    public static ReplayStore? From(AgentOptions options) => options.ReplayDirectory is { } directory ? new ReplayStore(directory) : null;

    /// <summary>
    /// A trial's recording starts afresh (F18 step 5a): what an earlier attempt of it wrote is removed. A retried trial records under the same key.
    /// <para>
    /// A known gap, rare: the earlier attempt's session may still be running or closing (a dropped connection is why it was retried), and a late write
    /// of its own lands in the index after this clear. Its ask would then sit beside the second attempt's, and an ask is answered from the first recording
    /// of it. It needs the old session to keep working after its client has gone, so it is left as it is; if it ever shows, key the index by session.
    /// </para>
    /// </summary>
    public async Task StartTrialAsync(string trial, CancellationToken cancellationToken = default)
    {
        string path = TrialFileName.Under(Trials, TrialFileName.Of(trial) + ".jsonl");
        await _indexLock.WaitAsync(cancellationToken);
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        finally
        {
            _indexLock.Release();
        }
    }

    public async Task SaveAnswerAsync<T>(string trial, ReplayAsk ask, T answer, CancellationToken cancellationToken = default)
    {
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(answer, Json);
        string hash = Convert.ToHexStringLower(SHA256.HashData(bytes));
        await WriteAnswerOnceAsync(hash, bytes, cancellationToken);
        await AppendAsync(new ReplayIndexLine(trial, ask.Kind, ask.Key, _clock.GetUtcNow(), hash, null), cancellationToken);
    }

    public Task SaveFailureAsync(string trial, ReplayAsk ask, string error, CancellationToken cancellationToken = default) =>
        AppendAsync(new ReplayIndexLine(trial, ask.Kind, ask.Key, _clock.GetUtcNow(), null, error), cancellationToken);

    /// <summary>The first recording of <paramref name="ask"/> in the trial, or null when there is none (or no such trial).</summary>
    public async Task<ReplayAnswer<T>?> FindAsync<T>(string trial, ReplayAsk ask, CancellationToken cancellationToken = default)
    {
        string index = TrialFileName.Under(Trials, TrialFileName.Of(trial) + ".jsonl");
        if (!File.Exists(index)) return null;

        foreach (string text in await File.ReadAllLinesAsync(index, cancellationToken))
        {
            if (ParseLine(text) is not { } line || line.Ask != ask) continue;
            if (line.Error is not null) return new ReplayAnswer<T>.Failed(line.Error);
            if (await ReadAnswerAsync<T>(line.Hash, cancellationToken) is { } answered) return answered;
        }
        return null;
    }

    /// <summary>
    /// Every distinct ask of <paramref name="kind"/> recorded in the trial, in the order recorded; empty when there is none (or no such
    /// trial). A miss compares its request with these to say what changed (F18 step 4e).
    /// </summary>
    public async Task<IReadOnlyList<ReplayAsk>> AsksAsync(string trial, ReplayKind kind, CancellationToken cancellationToken = default)
    {
        string index = TrialFileName.Under(Trials, TrialFileName.Of(trial) + ".jsonl");
        if (!File.Exists(index)) return [];

        return (await File.ReadAllLinesAsync(index, cancellationToken))
            .Select(ParseLine).OfType<ReplayIndexLine>().Where(line => line.Kind == kind).Select(line => line.Ask).Distinct().ToList();
    }

    private static ReplayIndexLine? ParseLine(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        try { return JsonSerializer.Deserialize<ReplayIndexLine>(text, Json); }
        catch (JsonException) { return null; }
    }

    /// <summary>
    /// The answer a hash names; the hash comes from a file, so it must be 64 lowercase hex digits before it is part of a path (ADR 0004).
    /// Returns the wrapper, never a bare <typeparamref name="T"/>: for a struct, "no file" as <c>T?</c> would be <c>default</c>, a real value.
    /// </summary>
    private async Task<ReplayAnswer<T>.Answered?> ReadAnswerAsync<T>(string? hash, CancellationToken cancellationToken)
    {
        if (hash is not { Length: 64 } || !hash.All(char.IsAsciiHexDigitLower)) return null;
        string path = Path.Combine(Answers, hash + ".json");
        if (!File.Exists(path)) return null;
        return JsonSerializer.Deserialize<T>(await File.ReadAllBytesAsync(path, cancellationToken), Json) is { } value ? new ReplayAnswer<T>.Answered(value) : null;
    }

    /// <summary>Written to a temporary name and moved into place; a hash that is already there is skipped, and a racing writer is no error.</summary>
    private async Task WriteAnswerOnceAsync(string hash, byte[] bytes, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Answers);
        string path = Path.Combine(Answers, hash + ".json");
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
