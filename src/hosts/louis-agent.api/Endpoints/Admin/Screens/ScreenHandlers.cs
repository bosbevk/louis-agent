using LouisAgent.Api.Contracts;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Screens;
using LouisAgent.Api.Messages;
using LouisAgent.Choices;
using LouisAgent.Composition.Messages;
using LouisAgent.Composition.Packs.Screens;
using LouisAgent.Composition.Packs.Screens.Reading;
using LouisAgent.Core.Hosting;
using Microsoft.AspNetCore.Http.HttpResults;
using RowsAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.Admin.Screens.ScreenPageReply>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;

namespace LouisAgent.Api.Endpoints.Admin.Screens;

/// <summary>
/// The screens the packs describe: the list for the sidebar, and one page of a screen's rows. The API knows no pack: it
/// finds a screen by name in the catalogue and hands back what it says (ADR 0027).
/// </summary>
/// <remarks>Named endpoints returning <c>TypedResults</c> (ADR 0045, 0051), guard clauses first.</remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
internal static class ScreenHandlers
{
    /// <summary>Every screen's description, in the order the packs list them.</summary>
    public static JsonHttpResult<ScreensReply> GetScreens(AgentEngineFactory engines) =>
        TypedResults.Json(new ScreensReply([.. engines.Catalog.Screens.Select(screen => ReplyOf(screen.Spec))]),
            AdminJson.SnakeCase);

    /// <summary>One page of a screen's rows; a page before the first is the first.</summary>
    public static async Task<RowsAnswer> GetRows(string name, string? search, int? page, AgentEngineFactory engines,
        CancellationToken cancellationToken)
    {
        if (engines.Catalog.Screen(name) is not { } screen)
            return ApiErrors.NotFound(MessageText.Format(ApiUserText.UnknownScreen, new { Name = name }));

        var found = await screen.ReadAsync(new ScreenQuery(search, Math.Max(page ?? 1, 1)), cancellationToken);
        return TypedResults.Json(
            new ScreenPageReply([.. found.Rows.Select(row => new ScreenRowReply(row.Id, row.Cells))], found.Total,
                ScreenQuery.PageSize), AdminJson.SnakeCase);
    }

    /// <summary>One screen's description as the page reads it.</summary>
    private static ScreenReply ReplyOf(ScreenSpec spec) =>
        new(spec.Name, spec.Title, spec.Description,
            [.. spec.Columns.Select(column => new ScreenColumnReply(column.Title, column.Kind.Wire()))],
            spec.Searchable)
        {
            Actions =
            [
                .. spec.Actions.Select(action =>
                    new ScreenActionReply(action.Name, action.Label, action.On.Wire(), action.Confirm))
            ]
        };
}
