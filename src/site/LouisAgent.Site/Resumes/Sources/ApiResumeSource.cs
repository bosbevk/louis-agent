using System.Net.Http.Json;
using System.Text.Json;
using LouisAgent.Site.Content;

namespace LouisAgent.Site.Resumes.Sources;

/// <summary>
/// The CV from the site's own API (F142), which holds the copy the admin's editor saved. When the API can't answer
/// (it is down, the client has no address, the answer isn't a CV), the CV is the one the site was built with, so the
/// page is never empty.
/// </summary>
/// <remarks>
/// A fallback, not a lenient reader: the answer is still read strictly, and one that doesn't fit is set aside whole
/// for the built-in CV, never half-read.
/// </remarks>
public sealed class ApiResumeSource(HttpClient http) : IResumeSource
{
    /// <summary>The API's copy, or the built-in CV when it can't be had.</summary>
    public async Task<Resume> ReadAsync()
    {
        // A client with no address is the server's own, with nowhere to ask.
        if (http.BaseAddress is null) return Cv.Resume;

        try
        {
            return await http.GetFromJsonAsync<Resume>(ResumeAddress.Read, ResumeReader.Options) ?? Cv.Resume;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or OperationCanceledException)
        {
            return Cv.Resume;
        }
    }
}
