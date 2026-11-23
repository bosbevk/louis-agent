using System.Text.Json;
using LouisAgent.Composition.Messages;
using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Evals.Contracts.Runs.Json;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Messages.Holes.Runs;

namespace LouisAgent.Evals.Runs.Store;

/// <summary>
/// Keeps each run as one JSON file in its folder, named by the run's id, so a person can open a run in their editor
/// and a folder listing is already in order. A file is written beside itself and moved into place, so a reader never
/// meets half a run, and a run is changed in place under the same lock, so two changes at once both stay.
/// </summary>
/// <remarks>
/// Fowler's Repository: whoever asks for runs doesn't know they are files. One file per run was picked over MongoDB
/// and over both behind one interface (Louis, 2026-11-23). An id is read by <see cref="RunId"/> before it is part
/// of a path (ADR 0004), and the reader is <see cref="RunJson"/>, strict as every reader of ours.
/// </remarks>
/// <param name="folder">The runs' folder, made on the first save.</param>
/// <seealso href="https://martinfowler.com/eaaCatalog/repository.html">Fowler: Repository</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0004-arguments-as-data.md">ADR 0004: arguments as data</seealso>
public sealed class RunStore(string folder)
{
    private const string Extension = ".json";
    private const string Draft = ".saving";

    private readonly Lock _gate = new();

    /// <summary>Saves the run, in place of the one with its id if there was one.</summary>
    public void Save(EvalRun run)
    {
        string path = PathOf(run.Id);
        lock (_gate)
        {
            Directory.CreateDirectory(folder);
            // Written beside the file and moved over it, so a read never meets half a run.
            string draft = path + Draft;
            File.WriteAllText(draft, JsonSerializer.Serialize(run, RunJson.Options));
            File.Move(draft, path, overwrite: true);
        }
    }

    /// <summary>The run with this id, or null when there is none.</summary>
    public EvalRun? Find(string id)
    {
        string path = PathOf(id);
        lock (_gate)
        {
            return File.Exists(path) ? Read(path) : null;
        }
    }

    /// <summary>
    /// Reads the run, changes it and saves what the change gives back, as one step under the store's lock, so two
    /// changes at once can't lose one. False, and nothing saved, for a run it doesn't have or a change that gives none.
    /// </summary>
    public bool Change(string id, Func<EvalRun, EvalRun?> change)
    {
        string path = PathOf(id);
        lock (_gate)
        {
            if (!File.Exists(path) || change(Read(path)) is not { } changed) return false;

            Save(changed);
            return true;
        }
    }

    /// <summary>The eval's runs, the newest first.</summary>
    public IReadOnlyList<EvalRun> Of(string eval)
    {
        lock (_gate)
        {
            if (!Directory.Exists(folder)) return [];

            return Directory.EnumerateFiles(folder, "*" + Extension)
                .Where(path => IdOf(path).Eval == eval)
                .Select(Read)
                .OrderByDescending(run => run.StartedAt)
                .ToList();
        }
    }

    /// <summary>Where the run with this id is kept; an id that isn't a run's is refused first.</summary>
    private string PathOf(string id)
    {
        if (RunId.TryParse(id, out _)) return Path.Combine(folder, id + Extension);

        var notAnId = new NotARunIdHoles(id);
        throw new ArgumentException(MessageText.Format(RunMessage.NotARunId, notAnId), nameof(id));
    }

    /// <summary>The id a file in the folder is named for; a file named for none isn't a run.</summary>
    private static RunId IdOf(string path)
    {
        if (RunId.TryParse(Path.GetFileNameWithoutExtension(path), out var id)) return id;

        throw NotARun(path, MessageText.Format(RunMessage.NotNamedForARun));
    }

    /// <summary>The run in this file, read strictly; a file that isn't one is an error that names it.</summary>
    private static EvalRun Read(string path)
    {
        try
        {
            using var file = File.OpenRead(path);
            return JsonSerializer.Deserialize<EvalRun>(file, RunJson.Options)
                   ?? throw NotARun(path, MessageText.Format(RunMessage.HoldsNoRun));
        }
        catch (JsonException fault)
        {
            throw NotARun(path, fault.Message, fault);
        }
    }

    /// <summary>The error for a file in the folder that isn't a run, with its name and why.</summary>
    private static InvalidDataException NotARun(string path, string reason, Exception? inner = null)
    {
        var notARun = new NotARunHoles(Path.GetFileName(path), reason);
        return new InvalidDataException(MessageText.Format(RunMessage.NotARun, notARun), inner);
    }
}
