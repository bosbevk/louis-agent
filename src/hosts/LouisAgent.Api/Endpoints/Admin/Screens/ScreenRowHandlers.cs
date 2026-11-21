using LouisAgent.Api.Contracts;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Screens;
using LouisAgent.Api.Messages;
using LouisAgent.Choices;
using LouisAgent.Composition.Logging;
using LouisAgent.Composition.Messages;
using LouisAgent.Composition.Packs.Screens;
using LouisAgent.Composition.Packs.Screens.Editing;
using LouisAgent.Core.Hosting;
using Microsoft.AspNetCore.Http.HttpResults;
using RecordAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.Admin.Screens.ScreenRecordReply>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;
using SaveAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.Admin.Screens.ScreenAnswerReply>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;

namespace LouisAgent.Api.Endpoints.Admin.Screens;

/// <summary>
/// One row of a screen as a form, and saving it. The pack checks what's saved and says which field it refused; a screen
/// that doesn't say rows can be edited has no form, and every save is logged.
/// </summary>
/// <remarks>Named endpoints returning <c>TypedResults</c> (ADR 0045, 0051), guard clauses first.</remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0086-a-packs-screens-are-described.md">ADR 0086: a pack's screens are described</seealso>
internal static class ScreenRowHandlers
{
    /// <summary>The row as a form.</summary>
    public static async Task<RecordAnswer> GetRow(string name, string id, AgentEngineFactory engines,
        CancellationToken cancellationToken)
    {
        if (Editable(name, engines) is not { } screen)
            return ApiErrors.NotFound(MessageText.Format(ApiUserText.UnknownScreen, new { Name = name }));
        if (await screen.ReadRowAsync(id, cancellationToken) is not { } record)
            return ApiErrors.NotFound(MessageText.Format(ApiUserText.UnknownScreenRow, new { Name = name, Row = id }));

        return TypedResults.Json(new ScreenRecordReply(record.Title, [.. record.Fields.Select(ReplyOf)]), AdminJson.SnakeCase);
    }

    /// <summary>Saves the form, and hands back what the pack says came of it.</summary>
    public static async Task<SaveAnswer> PutRow(string name, string id, ScreenSaveRequest? body, AgentEngineFactory engines,
        ILoggerFactory logs, CancellationToken cancellationToken)
    {
        if (Editable(name, engines) is not { } screen)
            return ApiErrors.NotFound(MessageText.Format(ApiUserText.UnknownScreen, new { Name = name }));
        if (await screen.ReadRowAsync(id, cancellationToken) is null)
            return ApiErrors.NotFound(MessageText.Format(ApiUserText.UnknownScreenRow, new { Name = name, Row = id }));

        var values = new ScreenValues(body?.Values ?? new Dictionary<string, string?>(),
            body?.Rows ?? new Dictionary<string, IReadOnlyList<IReadOnlyList<string>>>());
        var answer = await screen.SaveRowAsync(id, values, cancellationToken);
        logs.CreateLogger(typeof(ScreenRowHandlers).FullName!).Log(LogLevel.Information, ApiLogMessage.ScreenRowSaved,
            new { Screen = name, Row = id, answer.Done });
        return TypedResults.Json(new ScreenAnswerReply(answer.Done, answer.Message, answer.FieldErrors), AdminJson.SnakeCase);
    }

    /// <summary>The screen by that name, if its rows can be edited.</summary>
    private static IDataScreen? Editable(string name, AgentEngineFactory engines) =>
        engines.Catalog.Screen(name) is { Spec.Editable: true } screen ? screen : null;

    /// <summary>One field as the page reads it.</summary>
    private static ScreenFieldReply ReplyOf(ScreenField field) =>
        new(field.Name, field.Label, field.Kind.Wire(), field.Value, field.ReadOnly, field.Required, field.RowColumns,
            field.Rows);
}
