using LouisAgent.Api.Contracts.Admin.Imports;

namespace LouisAgent.Web.Components.Imports;

/// <summary>
/// One upload on the uploads page (F55): the file the person chose for each of its slots, and the report of the last
/// preview or save. A preview needs every slot's file; a save needs a clean preview that changes something, and
/// choosing a file again forgets the report, so a save never writes what the person hasn't previewed.
/// </summary>
/// <remarks>
/// Presentation Model (Fowler): the page's state and its rules live in a plain class the tests drive, and the markup
/// only binds to it. A class, not a record: it changes as the person picks files (ADR 0040).
/// </remarks>
/// <seealso href="https://martinfowler.com/eaaDev/PresentationModel.html">Fowler: Presentation Model</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values, classes for things</seealso>
public sealed class ImportForm(ImportRow upload)
{
    /// <summary>The files chosen so far, by slot name.</summary>
    private readonly Dictionary<string, ImportFileBody> _files = new(StringComparer.Ordinal);

    /// <summary>The upload as the API lists it: its name, what it takes and its slots.</summary>
    public ImportRow Upload { get; } = upload;

    /// <summary>What the last preview or save said, or nothing before the first one.</summary>
    public ImportReportReply? Report { get; set; }

    /// <summary>Every slot has its file.</summary>
    public bool CanPreview => Upload.Files.All(slot => _files.ContainsKey(slot.Name));

    /// <summary>The last preview found no problems, would change something, and hasn't been saved yet.</summary>
    public bool CanSave => Report is { Problems.Count: 0, Changes: > 0, Saved: false };

    /// <summary>Takes the file chosen for a slot, and forgets the report the old file gave.</summary>
    public void Choose(string slot, string fileName, byte[] content)
    {
        _files[slot] = new ImportFileBody(slot, fileName, content);
        Report = null;
    }

    /// <summary>The name of the file chosen for a slot, or nothing yet.</summary>
    public string? FileName(string slot) => _files.GetValueOrDefault(slot)?.FileName;

    /// <summary>The body the preview and save routes take: the chosen files, in the slots' order.</summary>
    public ImportRequest Request() =>
        new(Upload.Files.Where(slot => _files.ContainsKey(slot.Name)).Select(slot => _files[slot.Name]).ToList());
}
