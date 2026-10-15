namespace LouisAgent.Api.Services;

/// <summary>One mode's conversations of a saved run (F27): the questions, the calls and the answers, as the check wrote them.</summary>
/// <param name="Name">The transcript's place in the run: <c>delegated</c>, or <c>run-2/puls-web</c> for a repeated run.</param>
public sealed record RunTranscript(string Name, string Markdown);
