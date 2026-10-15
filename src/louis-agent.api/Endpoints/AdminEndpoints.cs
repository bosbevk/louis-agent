using LouisAgent.Api.Messages;
using LouisAgent.Api.Services;
using LouisAgent.Core.Messages;

namespace LouisAgent.Api.Endpoints;

/// <summary>
/// The admin portal's endpoints (F27), all under <c>/admin</c> and all behind <see cref="AdminGate"/>: with no admin
/// key configured every call is 403 and says what to set; with one, a call must present it. Step 1: the checks and
/// the saved runs; the runs themselves come in step 3.
/// </summary>
internal static class AdminEndpoints
{
    public static void MapAdmin(this WebApplication app, BenchmarkCatalog catalog, string? adminKey)
    {
        var admin = app.MapGroup("/admin").AddEndpointFilter(async (context, next) =>
        {
            switch (AdminGate.Check(adminKey, AdminGate.Presented(context.HttpContext.Request)))
            {
                case AdminAccess.Closed:
                    return ApiErrors.Result(StatusCodes.Status403Forbidden, "permission_error", MessageText.Format(ApiUserText.AdminClosed));
                case AdminAccess.Unauthorized:
                    return ApiErrors.Result(StatusCodes.Status401Unauthorized, "authentication_error", MessageText.Format(ApiUserText.InvalidApiKey));
                default:
                    return await next(context);
            }
        });

        admin.MapGet("/benchmarks", () => Results.Json(new
        {
            checks = catalog.Checks.Select(c => new { id = c.Id, title = c.Title, script = c.Script }),
            runs = catalog.SavedRuns().Select(Row),
        }));

        admin.MapGet("/benchmarks/runs/{folder}", (string folder) => catalog.Read(folder) is { } run
            ? Results.Json(new { run = Row(run.Run), summary = run.Summary, readme = run.Readme, results = run.Results })
            : ApiErrors.Result(StatusCodes.Status404NotFound, "not_found_error", MessageText.Format(ApiUserText.NoRun, new { Folder = folder })));
    }

    private static object Row(SavedRun run) =>
        new { folder = run.Folder, check = run.Check, saved_at = run.SavedAt, repeats = run.Repeats, cost = run.Cost };
}
