using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Prompts;
using LouisAgent.Choices;
using LouisAgent.Core.Skills.Headers;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LouisAgent.Api.Endpoints.Admin.Prompts;

/// <summary>
/// The admin portal's list of prompts (CL27): every built-in prompt this instance has, core's and its identities' and
/// packs', as its header says it, so a person sees what each one is without opening the file.
/// </summary>
/// <remarks>
/// A named endpoint returning <c>TypedResults</c> (ADR 0045, 0051) over <see cref="PromptCatalog"/>, which read the
/// headers once when the host started; a static class, since the route shares nothing but the catalogue.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
internal static class PromptsHandlers
{
    /// <summary>Every built-in prompt with a header, by its path.</summary>
    public static JsonHttpResult<PromptsReply> GetPrompts() =>
        TypedResults.Json(new PromptsReply([.. PromptCatalog.All.Where(p => p.Header is not null).Select(RowOf)]),
            AdminJson.SnakeCase);

    /// <summary>The row for one prompt: its path and what its header says, the level by its wire name.</summary>
    private static PromptRow RowOf(PromptFile prompt) =>
        new(prompt.Path, prompt.Header!.Name, prompt.Header.Description, prompt.Header.Level?.Wire(),
            prompt.Header.Toolsets);
}
