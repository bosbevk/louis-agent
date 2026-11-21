using LouisAgent.Api.Contracts;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Screens;
using LouisAgent.Api.Messages;
using LouisAgent.Composition.Logging;
using LouisAgent.Composition.Messages;
using LouisAgent.Composition.Packs.Screens.Acting;
using LouisAgent.Core.Hosting;
using Microsoft.AspNetCore.Http.HttpResults;
using ActionAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.Admin.Screens.ScreenAnswerReply>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;

namespace LouisAgent.Api.Endpoints.Admin.Screens;

/// <summary>
/// Asks a screen to do one of the things its pack named. Only what the description lists can be asked for, and each one
/// is logged with its row, so whoever runs it can see what was done.
/// </summary>
/// <remarks>Named endpoints returning <c>TypedResults</c> (ADR 0045, 0051), guard clauses first.</remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0086-a-packs-screens-are-described.md">ADR 0086: a pack's screens are described</seealso>
internal static class ScreenActionHandlers
{
    /// <summary>Does the action and hands back what the pack says came of it.</summary>
    public static async Task<ActionAnswer> PostAction(string name, string action, ScreenActionRequest? body,
        AgentEngineFactory engines, ILoggerFactory logs, CancellationToken cancellationToken)
    {
        if (engines.Catalog.Screen(name) is not { } screen)
            return ApiErrors.NotFound(MessageText.Format(ApiUserText.UnknownScreen, new { Name = name }));
        if (screen.Spec.Actions.FirstOrDefault(offered => offered.Name == action) is not { } offered)
            return ApiErrors.NotFound(MessageText.Format(ApiUserText.UnknownScreenAction, new { Name = name, Action = action }));

        var rowId = body?.RowId;
        if (offered.On is ScreenActionOn.Row && string.IsNullOrWhiteSpace(rowId))
            return ApiErrors.BadRequest(MessageText.Format(ApiUserText.ScreenActionNeedsRow, new { Action = action }));

        var answer = await screen.ActAsync(action, rowId, cancellationToken);
        logs.CreateLogger(typeof(ScreenActionHandlers).FullName!).Log(LogLevel.Information, ApiLogMessage.ScreenAction,
            new { Screen = name, Action = action, Row = rowId ?? "-", answer.Done });
        return TypedResults.Json(new ScreenAnswerReply(answer.Done, answer.Message, answer.FieldErrors), AdminJson.SnakeCase);
    }
}
