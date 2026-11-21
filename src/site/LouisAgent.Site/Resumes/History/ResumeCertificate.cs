namespace LouisAgent.Site.Resumes.History;

/// <summary>A certificate on the CV (F142): what, from whom, when, and where to check it.</summary>
/// <param name="Name">The certificate.</param>
/// <param name="Issuer">Who gave it.</param>
/// <param name="Date">When, a year or a year and month.</param>
/// <param name="Url">Where to check it.</param>
public sealed record ResumeCertificate(string Name, string? Issuer = null, string? Date = null, string? Url = null);
