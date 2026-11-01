namespace LouisAgent.Api.Evals.Drafts;

/// <summary>An eval's drafts, oldest first: the samples and the transcripts, each in their own list.</summary>
public sealed record EvalDrafts(IReadOnlyList<SampleDraft> Samples, IReadOnlyList<TranscriptDraft> Transcripts);
