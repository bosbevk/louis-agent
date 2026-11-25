namespace LouisAgent.Core.Messages.Holes.Approvals;

/// <summary>A tool already approved under the name, and the file to remove first.</summary>
public sealed record ApprovedToolAlreadyThereHoles(string Name, string Path);
