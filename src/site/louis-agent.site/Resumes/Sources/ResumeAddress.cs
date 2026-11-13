namespace LouisAgent.Site.Resumes.Sources;

/// <summary>
/// Where the CV is found (F142), each address written once: the page and the editor read it at one, the editor saves
/// it at another, and the admin's link, card and home lead to the editor.
/// </summary>
public static class ResumeAddress
{
    /// <summary>The route the site's host serves the CV on, open to read.</summary>
    public const string Read = "v1/site/cv";

    /// <summary>The route that keeps an edited CV, behind the admin's key.</summary>
    public const string Save = "v1/admin/cv";

    /// <summary>The editor's page in the admin.</summary>
    public const string Editor = "admin/resume";
}
