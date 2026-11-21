namespace LouisAgent.Site.Resumes;

/// <summary>One fact in the CV's row under the name (F142): a number that says a lot, and what it counts.</summary>
/// <param name="Value">The number, as written, such as "20 years".</param>
/// <param name="Label">What it counts, such as "building business software in .NET".</param>
public sealed record ResumeFact(string Value, string Label);
