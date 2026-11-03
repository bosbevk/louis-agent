using LouisAgent.Api.Admin.Replies;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Studio;
using LouisAgent.Api.Evals.Studio;
using ChecksAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.Admin.Studio.CheckListReply>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;

namespace LouisAgent.Api.Endpoints.Admin.Studio;

/// <summary>
/// The studio's catalogue: every check type the runner's evals can name, with its settings' schema, and every reader.
/// When the runner couldn't list them, the page is told why.
/// </summary>
/// <remarks>
/// Introduce Parameter Object (Fowler), with the object owning the behaviour, as <c>EvalHandlers</c>: the route shares
/// the runner's catalogue, which isn't a service, so the route is a named method a test calls with no HTTP. It returns
/// <c>TypedResults</c> with its answers declared.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
internal sealed class StudioHandlers(CheckList checks)
{
    /// <summary>The runner's checks and readers, or a 503 with the runner's reason.</summary>
    public ChecksAnswer GetChecks()
    {
        var listed = checks.Read();
        if (listed.Failure is { } failure) return ApiErrors.Unavailable(failure);
        return TypedResults.Json(CheckListReply.Of(listed), AdminJson.SnakeCase);
    }
}
