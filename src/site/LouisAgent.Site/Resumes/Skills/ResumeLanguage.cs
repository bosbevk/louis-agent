namespace LouisAgent.Site.Resumes.Skills;

/// <summary>A language Louis speaks (F142), and how well, in his own words.</summary>
/// <param name="Language">The language.</param>
/// <param name="Fluency">How well, such as Native.</param>
public sealed record ResumeLanguage(string Language, string? Fluency = null);
