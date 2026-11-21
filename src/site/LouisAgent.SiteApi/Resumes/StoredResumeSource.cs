using LouisAgent.Site.Resumes;
using LouisAgent.Site.Resumes.Sources;

namespace LouisAgent.SiteApi.Resumes;

/// <summary>
/// The CV for a page the host renders itself (F142): straight from the store, so the HTML a search engine reads holds
/// the saved CV, the same one the browser then asks the API for.
/// </summary>
public sealed class StoredResumeSource(ResumeStore store) : IResumeSource
{
    /// <summary>The saved CV, or the built-in one when none was saved.</summary>
    public Task<Resume> ReadAsync() => Task.FromResult(store.Read());
}
