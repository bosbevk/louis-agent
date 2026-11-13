namespace LouisAgent.Site.Resumes.Sources;

/// <summary>
/// Where the CV is found (F142), each address written once: the page reads it at one, open to anyone. The admin's
/// editor and card read it and save it at another, behind the admin's key, so neither opens without it (FX153).
/// </summary>
public static class ResumeAddress
{
    /// <summary>The route the site's host serves the CV on, open to read: the public page's.</summary>
    public const string Read = "v1/site/cv";

    /// <summary>The admin's own route, behind its key: the editor reads its copy there, and saves it there.</summary>
    public const string Admin = "v1/admin/cv";

    /// <summary>The editor's page in the admin.</summary>
    public const string Editor = "admin/resume";
}
