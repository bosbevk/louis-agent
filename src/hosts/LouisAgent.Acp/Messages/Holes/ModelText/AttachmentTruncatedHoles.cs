namespace LouisAgent.Acp.Messages.Holes.ModelText;

/// <summary>How much of a long attachment the model is shown, and how long it is, both written for reading.</summary>
public sealed record AttachmentTruncatedHoles(string Shown, string Length);
