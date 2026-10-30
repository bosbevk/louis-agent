using System.Text.Json;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Datasets.Manifest;
using LouisAgent.Evals.Datasets.Scenarios;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Labels;
using LouisAgent.Evals.Messages;

namespace LouisAgent.Evals.Files;

/// <summary>
/// An eval that is a folder of files: its <c>eval.json</c> names it, its modes and its checks by catalogue type;
/// <c>Dataset/</c> beside it holds the scenarios and labels, as for an eval in C#. Nothing of its own is code: a
/// turn's facts are read from the engine's events, as for every eval.
/// </summary>
/// <remarks>
/// The second half of a Factory Method: <see cref="EvalFileReader"/> makes the record, <see cref="Load"/> makes the
/// eval from it. The checks are built in the file's order, each with the eval's folder and the checks before it
/// (<see cref="CheckContext"/>), and one the file renames is wrapped in a <see cref="NamedCheck"/>. The dataset loads
/// with those checks, so a key no check reads, or a key a check reads that no scenario sets, is refused as before.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0056-a-check-is-a-catalogue-entry.md">ADR 0056: a check is a catalogue entry</seealso>
public sealed class FileEval : IEval
{
    /// <summary>The folder beside <c>eval.json</c> that holds the dataset.</summary>
    private const string DatasetFolder = "Dataset";

    /// <summary>The folder beside <c>eval.json</c> that holds the reference and known-bad transcripts.</summary>
    private const string TranscriptsFolder = "Transcripts";

    /// <summary>Settings left out of an entry: its record's defaults.</summary>
    private static readonly JsonElement NoParameters = JsonDocument.Parse("{}").RootElement.Clone();

    /// <summary>What the file says.</summary>
    private readonly EvalFile _file;

    /// <summary>The scenarios, their manifest and a person's labels.</summary>
    private readonly Dataset _dataset;

    /// <summary>An eval from its file, its dataset, its checks and where its manual is.</summary>
    private FileEval(EvalFile file, Dataset dataset, IReadOnlyList<ICheck> checks, string? manual) =>
        (_file, _dataset, Checks, Manual) = (file, dataset, checks, manual);

    /// <summary>
    /// Reads the eval in <paramref name="folder"/>, building its checks from <paramref name="catalog"/>;
    /// <paramref name="source"/> is where the folder sits in the repository, which the manual's path starts from.
    /// </summary>
    public static FileEval Load(string folder, CheckCatalog catalog, string source)
    {
        var file = EvalFileReader.Read(folder);
        var checks = Build(file, folder, catalog);
        var dataset = DatasetLoader.Load(Path.Combine(folder, DatasetFolder), checks);
        return new FileEval(file, dataset, checks, file.Manual is null ? null : $"{source}/{file.Manual}")
        {
            Transcripts = Path.Combine(folder, TranscriptsFolder)
        };
    }

    /// <summary>
    /// The eval's reference and known-bad transcripts, in <c>Transcripts/</c> beside its file, which the conformance
    /// check reads.
    /// </summary>
    public string Transcripts { get; private init; } = "";

    /// <summary>The identity the eval proves, as its file names it.</summary>
    public string Identity => _file.Identity;

    /// <inheritdoc />
    public string Name => _file.Name;

    /// <inheritdoc />
    public int Version => _file.Version;

    /// <inheritdoc />
    public string Proves => _file.Proves;

    /// <inheritdoc />
    public string? Manual { get; }

    /// <inheritdoc />
    public IReadOnlyList<EvalScenario> Scenarios => _dataset.Scenarios;

    /// <inheritdoc />
    public DatasetManifest Dataset => _dataset.Manifest;

    /// <inheritdoc />
    public IReadOnlyList<LabeledAnswer> Labels => _dataset.Labels;

    /// <inheritdoc />
    public IReadOnlyList<ICheck> Checks { get; }

    /// <summary>The modes the file names, each a role and the toolsets its sessions open with.</summary>
    public ValueTask<IReadOnlyList<EvalMode>>
        ModesAsync(ISessionClient sessions, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyList<EvalMode>>(
            [.. _file.Modes.Select(mode => new EvalMode(mode.Name, mode.Role, mode.Toolsets))]);

    /// <summary>
    /// The file's checks in its order, each from its catalogue entry with the folder and the checks before it; a type
    /// the catalogue doesn't have is refused, naming the file and the types it does have.
    /// </summary>
    private static IReadOnlyList<ICheck> Build(EvalFile file, string folder, CheckCatalog catalog)
    {
        var built = new List<ICheck>();
        foreach (var entry in file.Checks)
        {
            var type = catalog.Find(entry.Type) ?? throw new InvalidDataException(MessageText.Format(
                EvalFileMessage.UnknownCheck, new
                {
                    File = Path.Combine(folder, "eval.json"),
                    entry.Type,
                    Supported = string.Join(", ", catalog.Listing().Select(line => line.Type))
                }));
            var check = type.Create(entry.Parameters ?? NoParameters, new CheckContext(folder, [.. built]));
            built.Add(entry.Name is { } name ? new NamedCheck(name, check) : check);
        }

        return built;
    }
}
