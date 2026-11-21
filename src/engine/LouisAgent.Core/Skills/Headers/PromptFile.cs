namespace LouisAgent.Core.Skills.Headers;

/// <summary>
/// One prompt file, read (CL27): its path under <c>prompts/</c>, its header when it has one, and the body the model
/// reads.
/// </summary>
public sealed record PromptFile(string Path, PromptHeader? Header, string Body);
