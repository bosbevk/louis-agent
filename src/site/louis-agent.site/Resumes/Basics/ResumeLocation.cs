namespace LouisAgent.Site.Resumes.Basics;

/// <summary>
/// Where Louis lives (F142): a town and a region, never a street address, which a public page must not show.
/// </summary>
/// <param name="City">The town.</param>
/// <param name="Region">The province.</param>
/// <param name="CountryCode">The two-letter country code, such as ZA.</param>
public sealed record ResumeLocation(string? City = null, string? Region = null, string? CountryCode = null);
