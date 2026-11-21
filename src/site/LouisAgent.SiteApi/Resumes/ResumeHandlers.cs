using System.Text.Json;
using LouisAgent.Api.Endpoints;
using LouisAgent.Composition.Messages;
using LouisAgent.Site.Resumes;
using LouisAgent.Site.Resumes.Privacy;
using LouisAgent.SiteApi.Messages;
using LouisAgent.SiteApi.Messages.Holes;
using Microsoft.AspNetCore.Http.HttpResults;
using SaveAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Site.Resumes.Resume>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;

namespace LouisAgent.SiteApi.Resumes;

/// <summary>
/// The CV's two routes (F142): anyone reads it, for the page and the guide; the admin's editor saves it. A save is
/// held to what the embedded file is held to: the CV's shape, strictly, and nothing a public page mustn't show.
/// </summary>
/// <remarks>
/// Named methods returning typed results (ADR 0051, 0045), the checks first as guard clauses, each stopping with the
/// API's own error shape, so the editor reads a refusal as it reads any other.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0051-flat-conditionals-and-named-endpoints.md">ADR 0051: flat conditionals and named endpoints</seealso>
public sealed class ResumeHandlers(ResumeStore store)
{
    /// <summary>The CV, as saved, or as the site was built with.</summary>
    public JsonHttpResult<Resume> Get() => TypedResults.Json(store.Read(), ResumeReader.Options);

    /// <summary>Saves the CV in the request, or says why it can't be kept.</summary>
    public async Task<SaveAnswer> PutAsync(HttpRequest request)
    {
        if (!store.CanSave) return ApiErrors.Unavailable(MessageText.Format(SiteApiUserText.CvNowhereToKeep));

        using var body = new StreamReader(request.Body);
        string json = await body.ReadToEndAsync();

        // What a public page mustn't show, before anything is kept.
        if (ResumePrivacy.Problems(json) is [var problem, ..])
            return ApiErrors.BadRequest(MessageText.Format(TextOf(problem)));

        // The CV's shape, strictly: a misspelt or a missing field is refused with the reader's own reason.
        if (TryRead(json, out string reason) is not { } resume)
        {
            var notRead = new CvNotReadHoles(reason);
            return ApiErrors.BadRequest(MessageText.Format(SiteApiUserText.CvNotRead, notRead));
        }

        store.Save(resume);
        return TypedResults.Json(resume, ResumeReader.Options);
    }

    /// <summary>The CV in this text, or none and the reader's reason when it isn't one.</summary>
    private static Resume? TryRead(string json, out string reason)
    {
        try
        {
            reason = "";
            return ResumeReader.Read(json);
        }
        catch (JsonException ex)
        {
            reason = ex.Message;
            return null;
        }
    }

    /// <summary>The words for each thing a public page mustn't show.</summary>
    /// <remarks>A switch on the problem, one arm each (ADR 0051).</remarks>
    private static SiteApiUserText TextOf(ResumePrivacyProblem problem) => problem switch
    {
        ResumePrivacyProblem.IdNumber => SiteApiUserText.CvHoldsIdNumber,
        ResumePrivacyProblem.Salary => SiteApiUserText.CvHoldsSalary,
        ResumePrivacyProblem.BirthDate => SiteApiUserText.CvHoldsBirthDate,
        _ => SiteApiUserText.CvHoldsAddress
    };
}
