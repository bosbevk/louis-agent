namespace LouisAgent.Core.Messages.Holes.SkillFiles;

/// <summary>A skill file waiting from an earlier session, which this one hasn't loaded.</summary>
public sealed record PendingSkillFileNotLoadedHoles(string Name);
