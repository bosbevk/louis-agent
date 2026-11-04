using LouisAgent.Composition.Messages;
using LouisAgent.Composition.Packs.Imports;
using LouisAgent.Api.Contracts;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Imports;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Messages;
using Microsoft.AspNetCore.Http.HttpResults;
using ImportAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.Admin.Imports.ImportReportReply>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;

namespace LouisAgent.Api.Endpoints.Admin.Imports;

/// <summary>
/// The admin portal's uploads (F55): the list of what the packs take, and an upload's preview and save. The API knows
/// no pack: it finds the upload by name in the catalogue and passes the files on (ADR 0027).
/// </summary>
/// <remarks>
/// Named endpoints returning <c>TypedResults</c> (ADR 0045, 0051), guard clauses first. The files come as JSON with
/// their bytes in base64, as a session's attachments do, so the page needs no form and the API no antiforgery.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
internal static class ImportHandlers
{
    /// <summary>The most an upload's files may come to, all together: Stats SA's two come to about 1.1 MB.</summary>
    internal const int MaxMegabytes = 20;

    /// <summary>Every upload the packs take, with its files.</summary>
    public static JsonHttpResult<ImportsReply> GetImports(AgentEngineFactory engines) =>
        TypedResults.Json(new ImportsReply([.. engines.Catalog.Imports.Select(RowOf)]), AdminJson.SnakeCase);

    /// <summary>What saving the files would store, with nothing saved.</summary>
    public static Task<ImportAnswer> PostPreview(string name, ImportRequest? body, AgentEngineFactory engines,
        CancellationToken cancellationToken) =>
        Run(name, body, engines, (import, files) => import.PreviewAsync(files, cancellationToken));

    /// <summary>Saves what the preview listed.</summary>
    public static Task<ImportAnswer> PostSave(string name, ImportRequest? body, AgentEngineFactory engines,
        CancellationToken cancellationToken) =>
        Run(name, body, engines, (import, files) => import.SaveAsync(files, cancellationToken));

    /// <summary>Finds the upload, checks the files, and hands them to it.</summary>
    private static async Task<ImportAnswer> Run(string name, ImportRequest? body, AgentEngineFactory engines,
        Func<IDataImport, IReadOnlyList<ImportFile>, Task<ImportReport>> act)
    {
        if (engines.Catalog.Import(name) is not { } import)
            return ApiErrors.NotFound(MessageText.Format(ApiUserText.UnknownImport, new { Name = name }));
        if (body?.Files is not { Count: > 0 } files)
            return ApiErrors.BadRequest(MessageText.Format(ApiUserText.ImportNoFiles));

        long bytes = files.Sum(file => (long)(file.Content?.Length ?? 0));
        if (bytes > MaxMegabytes * 1024L * 1024L)
            return ApiErrors.TooLarge(MessageText.Format(ApiUserText.ImportTooLarge,
                new { Megabytes = bytes / (1024 * 1024), Max = MaxMegabytes }));

        var report = await act(import, [.. files.Select(f => new ImportFile(f.Slot, f.FileName, f.Content ?? []))]);
        return TypedResults.Json(ReplyOf(report), AdminJson.SnakeCase);
    }

    /// <summary>The row for one upload.</summary>
    private static ImportRow RowOf(IDataImport import) =>
        new(import.Name, import.Description, [.. import.Files.Select(slot => new ImportSlotRow(slot.Name, slot.Description))]);

    /// <summary>The reply for a report.</summary>
    private static ImportReportReply ReplyOf(ImportReport report) =>
        new(report.Period, report.Changes, report.Saved,
            [.. report.Facts.Select(fact => new ImportFactRow(fact.Label, fact.Value))], report.Problems);
}
