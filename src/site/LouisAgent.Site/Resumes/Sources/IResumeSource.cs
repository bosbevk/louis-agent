namespace LouisAgent.Site.Resumes.Sources;

/// <summary>
/// Where a page gets the CV (F142). In the browser it is the site's API; on the server, where a page is rendered for
/// a search engine, the host hands over its own, which reads the saved file directly.
/// </summary>
/// <remarks>
/// Strategy (Gang of Four), picked by where the page runs: the page asks one question and never branches on browser
/// or server. Without it the server's render would show the built-in CV while the browser shows the saved one.
/// </remarks>
/// <seealso href="https://refactoring.guru/design-patterns/strategy">Strategy (Refactoring.Guru)</seealso>
public interface IResumeSource
{
    /// <summary>The CV to show.</summary>
    Task<Resume> ReadAsync();
}
